namespace Lyt.AddAnyItem;

using System.Globalization;
using System.Threading;
using static ClientContextKey;

/// <summary> AddAnyItemCommand handler. </summary>
/// <remarks> Initializes a new instance of the <see cref="AddAnyItemCommand"/> class. </remarks>
/// <param name="traceSource">Trace source instance to utilize.</param>
[VisualStudioContribution]
// Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable VSEXTPREVIEW_OUTPUTWINDOW 
public class AddAnyItemCommand(TraceSource traceSource) : Command
{
    private static readonly Guid ContextMenuGuid = new("{d309f791-903f-11d0-9efc-00a0c911004f}");

    private readonly TraceSource logger = Requires.NotNull(traceSource, nameof(traceSource));

    private OutputWindow? outputWindow;

    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration => new("%Lyt.AddAnyItem.AddAnyItemCommand.DisplayName%")
    {
        // Use this object initializer to set optional parameters for the command. The required parameter,
        // displayName, is set above. DisplayName is localized and references an entry in .vsextension\string-resources.json.
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
        this.outputWindow = await this.Extensibility.Views().Output.GetChannelAsync("Output Window", "Output Window", cancellationToken);
        if (this.outputWindow is null)
        {
            Debug.WriteLine("Failed to open output window");
        }
        await base.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        if (context is not ClientContext clientContext)
        {
            return;
        }

        IServiceBroker serviceBroker = context.Extensibility.ServiceBroker;
        Uri selectedPathUri = await clientContext.GetSelectedPathAsync(cancellationToken);
        if ( !selectedPathUri.IsFile)
        {
            await this.OutputWriteLineAsync("Invalid Uri: " + selectedPathUri.ToString());
        }

        await this.OutputWriteLineAsync("Uri: " + selectedPathUri.ToString());
        ProjectQueryableSpace workspace = new(serviceBroker: serviceBroker, joinableTaskContext: null);
        var projectInfo = await this.CreateProjectInfoAsync(workspace, selectedPathUri, cancellationToken);
        if (!projectInfo.IsValid)
        {
            await this.OutputWriteLineAsync("Uri: " + selectedPathUri.ToString());
        }

        // await this.Extensibility.Shell().ShowPromptAsync("Hello from an extension!", PromptOptions.OK, cancellationToken);
    }

    private async Task<ProjectInfo> CreateProjectInfoAsync (ProjectQueryableSpace workspace, Uri selectedPathUri, CancellationToken cancellationToken)
    {
        ProjectInfo projectInfo = new ();

        if (!selectedPathUri.IsFile)
        {
            await this.OutputWriteLineAsync("Invalid Uri: " + selectedPathUri.ToString());
            return projectInfo;
        }

        bool foundTargetDirectory = false; 
        string selectedPath = selectedPathUri.LocalPath;
        DirectoryInfo selectedDirectory = new (selectedPath);
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

        StringBuilder message = new($"\n \n === Querying === \n");

        var solutionQuery = await this.Extensibility.Workspaces().QuerySolutionAsync(
            solution => solution.With(solution=> solution.Directory),
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

        var projects = await this.Extensibility.Workspaces().QueryProjectsAsync(
            project => project.With(project => project.Name)
                              .With(project => project.Path)
                              .With(project => project.Files.With(file => file.FileName).With(file => file.Path)),
            cancellationToken);
        foreach (var project in projects)
        {
            _ = message.Append(CultureInfo.CurrentCulture, $"{project.Name} \t {project.Path}\n");

            foreach (var file in project.Files)
            {
                _ = message.Append(CultureInfo.CurrentCulture, $" \t {file.FileName} \t {file.Path}\n");
            }
        }

        Debug.WriteLine(message);

        return projectInfo;
    }

    private async Task OutputWriteLineAsync (string message)
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

    private sealed record class ProjectInfo ()
    {
        public bool IsValid { get; set; } = false;

        public string SelectedDirectory { get; set; } = "";

        public string SolutionDirectory { get; set; } = "";
    }
}
#pragma warning restore VSEXTPREVIEW_OUTPUTWINDOW // Type is for evaluation purposes only and is subject to change or removal in future updates.
