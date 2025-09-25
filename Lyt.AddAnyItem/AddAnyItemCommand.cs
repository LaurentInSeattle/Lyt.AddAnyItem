namespace Lyt.AddAnyItem;

using static ClientContextKey;

/// <summary> AddAnyItemCommand handler. </summary>
[VisualStudioContribution]
// Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable VSEXTPREVIEW_OUTPUTWINDOW 
public class AddAnyItemCommand : Command
{
    private readonly TraceSource logger;
    private OutputWindow? outputChannel;

    /// <summary> Initializes a new instance of the <see cref="AddAnyItemCommand"/> class. </summary>
    /// <param name="traceSource">Trace source instance to utilize.</param>
    public AddAnyItemCommand(TraceSource traceSource)
    {
        // This optional TraceSource can be used for logging in the command.
        // You can use dependency injection to access other services here as well.
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
    }

    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration => new("%Lyt.AddAnyItem.AddAnyItemCommand.DisplayName%")
    {
        // Use this object initializer to set optional parameters for the command. The required parameter,
        // displayName, is set above. DisplayName is localized and references an entry in .vsextension\string-resources.json.
        Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
        Placements =
        [
            CommandPlacement.VsctParent(new Guid("{d309f791-903f-11d0-9efc-00a0c911004f}"), id: 521, priority: 0), // File in project context menu
            CommandPlacement.VsctParent(new Guid("{d309f791-903f-11d0-9efc-00a0c911004f}"), id: 523, priority: 0), // Folder in project context menu
            // CommandPlacement.VsctParent(new Guid("{d309f791-903f-11d0-9efc-00a0c911004f}"), id: 518, priority: 0), // Project context menu
            // CommandPlacement.VsctParent(new Guid("{d309f791-903f-11d0-9efc-00a0c911004f}"), id: 537, priority: 0), // Solution context menu
            // CommandPlacement.KnownPlacements.ExtensionsMenu
        ],
    };

    /// <inheritdoc />
    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Use InitializeAsync for any one-time setup or initialization.
        this.outputChannel = await this.Extensibility.Views().Output.GetChannelAsync("Output Window", "Output Window", cancellationToken);
        await base.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        if (context is not ClientContext clientContext)
        {
            return;
        }

        ClientContextKey contextKey = Shell.ActiveSelectionPath;
        Uri uri = await clientContext.GetSelectedPathAsync(cancellationToken);
        Debug.WriteLine(uri.ToString());
        if (this.outputChannel != null)
        {
            await this.outputChannel.Writer.WriteLineAsync("This is a test of the output window.");
        }

        await this.Extensibility.Shell().ShowPromptAsync("Hello from an extension!", PromptOptions.OK, cancellationToken);
    }
}
#pragma warning restore VSEXTPREVIEW_OUTPUTWINDOW // Type is for evaluation purposes only and is subject to change or removal in future updates.
