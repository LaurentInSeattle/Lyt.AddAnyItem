namespace Lyt.AddAnyItem; 

/// <summary> Extension entrypoint for the VisualStudio.Extensibility extension. </summary>
[VisualStudioContribution]
internal class ExtensionEntrypoint : Extension
{
    /// <inheritdoc/>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
                id: "Lyt.AddAnyItem.0fd1ae9a-42d9-41f9-8c8d-401c0bca5f2e",
                version: this.ExtensionAssemblyVersion,
                publisherName: "Laurent Yves Testud",
                displayName: "Lyt.AddAnyItem",
                description: "Extension allowing Visual Studion users to add any kind of custom items to a project."),
    };

    /// <inheritdoc />
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);

        // You can configure dependency injection here by adding services to the serviceCollection.
    }
}
