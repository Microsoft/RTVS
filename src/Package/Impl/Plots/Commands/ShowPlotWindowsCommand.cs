﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Utilities;
using Microsoft.VisualStudio.R.Packages.R;

namespace Microsoft.VisualStudio.R.Package.Plots.Commands {
    internal sealed class ShowPlotWindowsCommand : PackageCommand {
        public ShowPlotWindowsCommand() :
            base(RGuidList.RCmdSetGuid, RPackageCommandId.icmdShowPlotWindow) { }

        internal override void Handle() {
            // TODO: find ad show all windows
            ToolWindowUtilities.ShowWindowPane<PlotWindowPane>(0, true);
        }
    }
}
