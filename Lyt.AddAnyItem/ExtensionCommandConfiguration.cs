// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lyt.AddAnyItem;

internal static class ExtensionCommandConfiguration
{
    [VisualStudioContribution]
    public static ToolbarConfiguration ToolBar => new("%Lyt.AddAnyItem.AddAnyItemCommand.DisplayName%")
    {
        Children =
        [
            ToolbarChild.Command<AddAnyItemCommand>(),
        ],
    };
}
