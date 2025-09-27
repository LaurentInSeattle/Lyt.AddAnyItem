namespace Lyt.AddAnyItem;

public sealed class ProjectInformation()
{
    public bool IsValid { get; set; } = false;

    public string SelectedDirectory { get; set; } = "";

    public string SolutionDirectory { get; set; } = "";

    public string ProjectFolder { get; set; } = "";

    public string ProjectName { get; set; } = "";

    public string ProjectNamespace { get; set; } = "";

    public bool Validate()
    {
        // TODO ! 
        this.IsValid = true;
        return this.IsValid;
    }
}
