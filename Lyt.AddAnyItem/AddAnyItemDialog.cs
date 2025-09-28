namespace Lyt.AddAnyItem;

/// <summary> A remote user control to use as dialog UI. </summary>
internal class AddAnyItemDialog : RemoteUserControl
{
    /// <summary> Initializes a new instance of the <see cref="AddAnyItemDialog" /> class. </summary>
    /// <param name="dataContext">
    /// Data context of the remote control which can be referenced from xaml through data binding.
    /// </param>
    /// <param name="synchronizationContext">
    /// Optional synchronizationContext that the extender can provide to ensure that <see cref="IAsyncCommand"/>
    /// are executed and properties are read and updated from the extension main thread.
    /// </param>
    public AddAnyItemDialog(object? dataContext, SynchronizationContext? synchronizationContext = null)
        : base(dataContext, synchronizationContext)
    {
        // BROKEN STUFF! 
        //
        // Line below present in the sample code, but does not compile when using the latest nugets
        // => base.ResourceDictionaries.AddEmbeddedResource("Lyt.AddAnyItem.Resources.DialogResources.xaml");
        // Dialog still showing up though...
    }
}
