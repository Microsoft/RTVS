﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Packages.R;

namespace Microsoft.VisualStudio.R.Package.Repl.Workspace {
    internal sealed class ShowRInteractiveWindowsCommand : PackageCommand {
        public ShowRInteractiveWindowsCommand() :
            base(RGuidList.RCmdSetGuid, RPackageCommandId.icmdShowReplWindow) {
        }
        internal override void Handle() {
            RPackage.Current.InteractiveWindowProvider.Open(instanceId: 0, focus: true);
        }
    }
}
