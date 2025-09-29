namespace Lyt.AddAnyItem;

/// <summary> A remote user control to use as dialog UI. </summary>
/// <remarks> Initializes a new instance of the <see cref="AddAnyItemDialog" /> class. </remarks>
/// <param name="dataContext"> Data context of the remote control which can be referenced from xaml through data binding.</param>
/// <param name="synchronizationContext">
/// Optional synchronizationContext that the extender can provide to ensure that <see cref="IAsyncCommand"/>
/// are executed and properties are read and updated from the extension main thread.
/// </param>
internal class AddAnyItemDialog(object? dataContext, SynchronizationContext? synchronizationContext = null) : 
    RemoteUserControl(dataContext, synchronizationContext)
{
    private const string OrgFolderName = "Lyt";
    private const string TemplatesFolderName = "AddAnyItem";

    public override Task ControlLoadedAsync(CancellationToken cancellationToken)
    {
        _ = base.ControlLoadedAsync(cancellationToken);

        //this.SelectedItemKind = "Avalonia View ViewModel";
        //this.SelectedItemName = "Shell";

        this.Populate(); 
        return Task.CompletedTask;
    }

    public string Message { get; private set; } = string.Empty;

    public string TemplatesFolderPath { get; private set; } = string.Empty;

    public string FolderPath { get; private set; } = string.Empty;

    public string SelectedItemKind { get; private set; } = string.Empty;

    public string SelectedItemName { get; private set; } = string.Empty;

    private void Populate()
    {
        this.FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        string orgFolderPath = Path.Combine(this.FolderPath, OrgFolderName);
        this.TemplatesFolderPath = Path.Combine(orgFolderPath, TemplatesFolderName);
        // this.SelectedFolderTextBlock.Text = this.TemplatesFolderPath;
    }

    private void PopulateTemplateFoldersComboBox ()
    {
        List<string> templateNames = this.EnumerateExistingTemplateFolders(out string message);
        if (templateNames.Count == 0)
        {
            this.Message = message;
            return;
        }
    }

    private List<string> EnumerateExistingTemplateFolders(out string message)
    {
        message = string.Empty;
        List<string> empty = [];
        List<string> templateFolders = [];
        try
        {
            // Make sure we have a templates folder 
            DirectoryInfo templatesDirectoryInfo = new(this.TemplatesFolderPath);
            if (!templatesDirectoryInfo.Exists)
            {
                message = "EnumerateExistingTemplateFolders: No templates folder";
                return empty;
            }

            // Enumerate directories 
            List<string> directoryPaths = this.TemplatesFolderPath.EnumerateDirectories();
            EnumerationOptions enumerationOptions = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
            };

            foreach (string directoryPath in directoryPaths)
            {
                // Assuminf that we have too few files to bother with creating threads 
                // Enumerate files and Make sure that there is at least one template file
                var files = directoryPath.EnumerateFiles(enumerationOptions);
                int validTemplateFilesCount = 0;
                foreach (string file in files)
                {
                    if (file.Contains(AddAnyItemCommand.TemplateNameKey, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ++validTemplateFilesCount;
                    }
                }

                if (validTemplateFilesCount > 0)
                {
                    DirectoryInfo directoryPathInfo = new(directoryPath);
                    templateFolders.Add(directoryPathInfo.Name);
                }
            }

            if (templateFolders.Count == 0)
            {
                message = "EnumerateExistingTemplateFolders: No valid templates \n";
                return empty;
            }

            return templateFolders;
        }
        catch (Exception ex)
        {
            message = "EnumerateExistingTemplateFolders: Exception thrown: \n" + ex.ToString();
            return empty;
        }
    }
}
