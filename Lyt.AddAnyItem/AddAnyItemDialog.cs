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
    public override Task ControlLoadedAsync(CancellationToken cancellationToken)
    {
        _ = base.ControlLoadedAsync(cancellationToken);

        if (this.DataContext is AddItemDialogModel addItemDialogModel)
        {
            addItemDialogModel.Populate();
        } 

        return Task.CompletedTask;
    }
}
