namespace Lyt.AddAnyItem;

using Microsoft.VisualStudio.RpcContracts.Notifications;

/// <summary> AddAnyItemCommand handler. </summary>
/// <remarks> Initializes a new instance of the <see cref="AddAnyItemCommand"/> class. </remarks>
/// <param name="traceSource">Trace source instance to utilize.</param>
[VisualStudioContribution]
// Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable VSEXTPREVIEW_OUTPUTWINDOW 
public class AddAnyItemCommand(TraceSource traceSource) : Command
{
    public const string TemplateNameKey = "{Name}";

    private const string GeneratedFolderName = "Generated";
    private const string TemplateNamespaceKey = "{Namespace}";

    private static readonly Guid ContextMenuGuid = new("{d309f791-903f-11d0-9efc-00a0c911004f}");

    private readonly TraceSource logger = Requires.NotNull(traceSource, nameof(traceSource));

    private OutputWindow? outputWindow;

    private string TemplatesFolderPath { get; set; } = string.Empty;

    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration =>
        new("%Lyt.AddAnyItem.AddAnyItemCommand.DisplayName%")
        {
            // Use this object initializer to set optional parameters for the command. The required parameter,
            // displayName, is set above.
            // DisplayName is localized and references an entry in .vsextension\string-resources.json.
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
            Placements =
            [
                CommandPlacement.VsctParent(ContextMenuGuid, id: 521, priority: 0), // File in project context menu
                CommandPlacement.VsctParent(ContextMenuGuid, id: 523, priority: 0), // Folder in project context menu
                // CommandPlacement.VsctParent(ContextMenuGuid, id: 518, priority: 0), // Project context menu
                // CommandPlacement.VsctParent(ContextMenuGuid, id: 537, priority: 0), // Solution context menu
                // CommandPlacement.KnownPlacements.ExtensionsMenu
            ],
        };

    /// <inheritdoc />
    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Use InitializeAsync for any one-time setup or initialization.
        this.outputWindow = await this.Extensibility.Views().Output.GetChannelAsync("Add Any Item", "Add Any Item", cancellationToken);
        if (this.outputWindow is null)
        {
            Debug.WriteLine("Failed to open output window");
        }

        await base.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (context is not ClientContext clientContext)
            {
                await this.OutputWriteLineAsync("Add Any Item Aborted: No Client Context");
                return;
            }

            Uri selectedPathUri = await clientContext.GetSelectedPathAsync(cancellationToken);
            if (!selectedPathUri.IsFile)
            {
                await this.OutputWriteLineAsync("Add Any Item Aborted: Invalid Uri: " + selectedPathUri.ToString());
                return;
            }

            await this.OutputWriteLineAsync("Uri: " + selectedPathUri.ToString());
            var workspaces = this.Extensibility.Workspaces();
            var projectInfo = await this.CreateProjectInfoAsync(workspaces, selectedPathUri, cancellationToken);
            if (!projectInfo.Validate())
            {
                await this.OutputWriteLineAsync("Uri: " + selectedPathUri.ToString());
                return;
            }

            // Ownership of the RemoteUserControl is transferred to VisualStudio,
            // so it should not be disposed by the extension
            var control = new AddAnyItemDialog(dataContext: null);
            string title = "Add Templates";
            DialogResult dialogResult = 
                await this.Extensibility.Shell().ShowDialogAsync(control, title, DialogOption.OKCancel, cancellationToken);
            if (dialogResult == DialogResult.Cancel)
            {
                await this.OutputWriteLineAsync("Add Any Item Cancelled" );
                return;
            }

            string selectedItemKind = control.SelectedItemKind;
            string selectedItemName = control.SelectedItemName;

            selectedItemKind = "Avalonia View ViewModel";
            selectedItemName = "Shell";

            bool filesGenerated = await this.GenerateFilesFromTemplatesAsync(projectInfo, selectedItemKind, selectedItemName);
            if (!filesGenerated)
            {
                await this.OutputWriteLineAsync("Add Any Item Aborted: Failed to create project files from templates: " + selectedPathUri.ToString());
                return;
            }

            string destinationProjectPath = projectInfo.SelectedDirectory;

            // Add a newly created source files to the selected project.
            async Task AddSourceFileAsync(string sourceFilePath)
            {
                FileInfo fileInfo = new(sourceFilePath);
                string content = File.ReadAllText(sourceFilePath);
                string sourceFileName = fileInfo.Name;
                string targetFilePath = Path.Combine(destinationProjectPath, sourceFileName);
                await workspaces.UpdateProjectsAsync(
                    project => project.Where(project => project.Name == projectInfo.ProjectName),
                    project => project.CreateFile(targetFilePath, content),
                    cancellationToken);
            }

            foreach (string file in projectInfo.GeneratedFiles)
            {
                await AddSourceFileAsync(file);
                await this.OutputWriteLineAsync("Add Any Item: Added to project: " + file);
            }

            // Save and Rebuild everything 
            IQueryResults<ISolutionSnapshot> solutions = await workspaces.QuerySolutionAsync(s => s, cancellationToken);
            foreach (var solution in solutions)
            {
                await solution.SaveAsync(cancellationToken);
                await solution.AsQueryable().BuildAsync(cancellationToken);
            }

            // success 
            await this.OutputWriteLineAsync("Add Item Complete: " + selectedItemKind + "  -  " + selectedItemName);
        }
        catch (Exception ex)
        {
            await this.OutputWriteLineAsync("Add Any Item Aborted: Exception thrown: \n" + ex.ToString());
        }
    }

    private async Task<ProjectInformation> CreateProjectInfoAsync(
        WorkspacesExtensibility workspaces, Uri selectedPathUri, CancellationToken cancellationToken)
    {
        ProjectInformation projectInfo = new();

        if (!selectedPathUri.IsFile)
        {
            await this.OutputWriteLineAsync("Invalid Uri: " + selectedPathUri.ToString());
            return projectInfo;
        }

        bool foundTargetDirectory = false;
        string selectedPath = selectedPathUri.LocalPath;
        DirectoryInfo selectedDirectory = new(selectedPath);
        if (selectedDirectory.Exists)
        {
            projectInfo.SelectedDirectory = selectedPath;
            foundTargetDirectory = true;
        }
        else
        {
            FileInfo fileInfo = new(selectedPath);
            if (fileInfo.Exists)
            {
                DirectoryInfo? directory = fileInfo.Directory;
                if (directory is not null && directory.Exists)
                {
                    projectInfo.SelectedDirectory = selectedPath;
                    foundTargetDirectory = true;
                }
            }
        }

        if (!foundTargetDirectory)
        {
            await this.OutputWriteLineAsync("Invalid Path: " + selectedPath);
            return projectInfo;
        }

        StringBuilder message = new($"\n \n  Querying Workspaces \n");
        var solutionQuery = await workspaces.QuerySolutionAsync(
            solution => solution.With(solution => solution.Directory),
            cancellationToken);
        ISolutionSnapshot? solutionSnapshot = solutionQuery.FirstOrDefault();
        if (solutionSnapshot is null)
        {
            return projectInfo;
        }

        _ = message.Append(CultureInfo.CurrentCulture, $"{solutionSnapshot.Directory}\n");
        if (string.IsNullOrWhiteSpace(solutionSnapshot.Directory))
        {
            return projectInfo;
        }

        projectInfo.SolutionDirectory = solutionSnapshot.Directory;

        bool foundProject = false;
        var projects = await workspaces.QueryProjectsAsync(
            project => project.With(project => project.Name)
                              .With(project => project.Path)
                              .With(project => project.DefaultNamespace)
                              .With(project => project.Files.With(file => file.FileName).With(file => file.Path)),
            cancellationToken);
        foreach (var project in projects)
        {
            _ = message.Append(CultureInfo.CurrentCulture, $"{project.Name} \t {project.Path}\n");
            FileInfo projectFileInfo = new(project.Path);
            if (!projectFileInfo.Exists)
            {
                break;
            }

            string? projectFolderPath = projectFileInfo.DirectoryName;
            if (string.IsNullOrWhiteSpace(projectFolderPath))
            {
                break;
            }

            if (projectInfo.SelectedDirectory.StartsWith(projectFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                projectInfo.ProjectFolder = projectFolderPath;
                projectInfo.ProjectName = project.Name;
                projectInfo.ProjectNamespace = project.DefaultNamespace;
                foundProject = true;
                break;
            }
        }

        if (!foundProject)
        {
            await this.OutputWriteLineAsync("Project not found: " + selectedPath);
            return projectInfo;
        }
        else
        {
            await this.OutputWriteLineAsync(
                "Project: " + projectInfo.ProjectName + "  Namespace: " + projectInfo.ProjectNamespace);
        }

        Debug.WriteLine(message);

        return projectInfo;
    }

    private async Task<bool> GenerateFilesFromTemplatesAsync(
        ProjectInformation projectInformation,
        string selectedItemKind,
        string selectedItemName)
    {
        if (!projectInformation.IsValid ||
            string.IsNullOrWhiteSpace(selectedItemKind) ||
            string.IsNullOrWhiteSpace(selectedItemName))
        {
            await this.OutputWriteLineAsync("GenerateFilesFromTemplates: Invalid parameters");
            return false;
        }

        try
        {
            // 1 - Make sure (maybe again) we have a templates folder 
            DirectoryInfo templatesDirectory = new(this.TemplatesFolderPath);
            if (!templatesDirectory.Exists)
            {
                await this.OutputWriteLineAsync("GenerateFilesFromTemplates: No templates folder");
                return false;
            }

            // 2 - Make sure we have a folder for the selected kind
            string selectedItemKindFolder = Path.Combine(this.TemplatesFolderPath, selectedItemKind);
            DirectoryInfo selectedItemKindDirectory = new(selectedItemKindFolder);
            if (!selectedItemKindDirectory.Exists)
            {
                await this.OutputWriteLineAsync("GenerateFilesFromTemplates: No template folder for selected kind");
                return false;
            }

            // 3 - Enumerate and Make sure that there is at least one template file
            EnumerationOptions enumerationOptions = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
            };

            var files = selectedItemKindFolder.EnumerateFiles(enumerationOptions);
            List<string> templateFiles = new(files.Count);
            foreach (string file in files)
            {
                if (file.Contains(TemplateNameKey, StringComparison.InvariantCultureIgnoreCase))
                {
                    templateFiles.Add(file);
                }
            }

            if (templateFiles.Count == 0)
            {
                await this.OutputWriteLineAsync("GenerateFilesFromTemplates: No template files for selected kind");
                return false;
            }

            // 4 - Delete folder for generated files if present 
            string selectedItemGeneratedFolder = Path.Combine(selectedItemKindFolder, GeneratedFolderName);
            DirectoryInfo generatedFolder = new(selectedItemGeneratedFolder);
            if (generatedFolder.Exists)
            {
                Directory.Delete(selectedItemGeneratedFolder, recursive: true);
            }

            // 5  - Create a new "fresh" folder for generated files 
            Directory.CreateDirectory(selectedItemGeneratedFolder);

            // 6 - Generate files 
            string GenerateFileFromTemplate(string sourceFile)
            {
                string sourceText = File.ReadAllText(sourceFile);
                string targetText = sourceText.Replace(TemplateNameKey, selectedItemName);
                string namespaceString = projectInformation.ProjectNamespace;
                // TODO: Add folder to name space 
                targetText = targetText.Replace(TemplateNamespaceKey, namespaceString);

                FileInfo fileInfo = new(sourceFile);
                string sourceFileName = fileInfo.Name;
                string targetFileName = sourceFileName.Replace(TemplateNameKey, selectedItemName);
                string targetPath = Path.Combine(selectedItemGeneratedFolder, targetFileName);
                File.WriteAllText(targetPath, targetText);
                return targetPath;
            }

            List<string> generatedFiles = new(templateFiles.Count);
            foreach (string file in templateFiles)
            {
                // To few files to bother with creating threads 
                string target = GenerateFileFromTemplate(file);
                if (!string.IsNullOrWhiteSpace(target))
                {
                    generatedFiles.Add(target);
                }
            }

            if (templateFiles.Count != generatedFiles.Count)
            {
                await this.OutputWriteLineAsync("GenerateFilesFromTemplates: Failed to generate some or all target files");
                return false;
            }

            projectInformation.GeneratedFiles = generatedFiles;
            return true;
        }
        catch (Exception ex)
        {
            await this.OutputWriteLineAsync("GenerateFilesFromTemplates: Exception thrown: \n" + ex.ToString());
            return false;
        }
    }

    private async Task OutputWriteLineAsync(string message)
    {
        if (this.outputWindow is null)
        {
            Debug.WriteLine("Failed to open output window");
            Debug.WriteLine(message);
            return;
        }

        Debug.WriteLine(message);
        await this.outputWindow.Writer.WriteLineAsync(message);
    }
}
#pragma warning restore VSEXTPREVIEW_OUTPUTWINDOW // Type is for evaluation purposes only and is subject to change or removal in future updates.
