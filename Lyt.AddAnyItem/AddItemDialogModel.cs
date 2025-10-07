namespace Lyt.AddAnyItem;

using Microsoft.VisualStudio.Extensibility.Shell.FileDialog;
using Microsoft.Win32;
using System.Threading;

[DataContract]
public partial class AddItemDialogModel
{
    private const string OrgFolderName = "Lyt";
    private const string TemplatesFolderName = "AddAnyItem";

    private readonly AddAnyItemCommand addAnyItemCommand; 

    [DataMember]
    public string SelectedFolderText { get; set; }

    [DataMember]
    public ObservableList<string> Names { get; set; } = [];

    [DataMember]
    public string SelectedItemKind { get; set; }

    [DataMember]
    public int SelectedIndexKind { get; set; }

    [DataMember]
    public string SelectedItemName { get; set; }

    [DataMember]
    public string Message { get; set; }

    [DataMember]
    public AsyncCommand ChangeFolderCommand { get; }

    [DataMember]
    public AsyncCommand SelectionChangedCommand { get; }

    [DataMember]
    public AsyncCommand TextChangedCommand { get; }

    public string TemplatesFolderPath { get; private set; } = string.Empty;

    public string FolderPath { get; private set; } = string.Empty;

    public AddItemDialogModel(AddAnyItemCommand addAnyItemCommand)
    {
        this.addAnyItemCommand = addAnyItemCommand;

        this.ChangeFolderCommand = new AsyncCommand(async (parameter, context, cancellationToken) =>
        {
            if ( parameter is AddItemDialogModel addItemDialogModel)
            {
                await addItemDialogModel.SelectFolderAsync(cancellationToken); 
            }
        });

        this.SelectionChangedCommand = new AsyncCommand((parameter, context, cancellationToken) =>
        {
            return Task.CompletedTask;
        });

        this.TextChangedCommand = new AsyncCommand((parameter, context, cancellationToken) =>
        {
            return Task.CompletedTask;
        });

        this.Message = string.Empty;
        this.SelectedFolderText = string.Empty;
        this.SelectedItemKind = string.Empty;
        this.SelectedItemName = string.Empty;

        this.FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        string orgFolderPath = Path.Combine(this.FolderPath, OrgFolderName);
        this.TemplatesFolderPath = Path.Combine(orgFolderPath, TemplatesFolderName);

        this.Populate();
    }

    public void Populate()
    {
        this.SelectedFolderText = this.TemplatesFolderPath;
        this.PopulateTemplateFoldersComboBox(); 
    }

    private async Task SelectFolderAsync(CancellationToken cancellationToken)
    {
        string personalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        FolderDialogOptions options = new()
        {
             InitialDirectory = personalFolder,
             Title = "Select a directory containing templates..."
        };

        string? folderPath = 
            await this.addAnyItemCommand.Extensibility.Shell().ShowOpenFolderDialogAsync(options, cancellationToken);
        Debug.WriteLine($"Selected folder path: {folderPath ?? "No selection"}");
        if (!string.IsNullOrWhiteSpace(folderPath) )
        {
            // For the rare case of user or someone delete the selected folder 
            DirectoryInfo folderPathDirectoryInfo = new(folderPath);
            if (!folderPathDirectoryInfo.Exists)
            {
                return;
            }

            this.TemplatesFolderPath = folderPath;
            this.Populate(); 
        }
    }

    private void PopulateTemplateFoldersComboBox()
    {
        List<string> templateNames = this.EnumerateExistingTemplateFolders(out string message);
        if (templateNames.Count == 0)
        {
            this.Message = message;
            return;
        }

        this.Names = new ObservableList<string>(templateNames);
        this.SelectedIndexKind = 0; 
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
