﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Packages.R;

namespace Microsoft.VisualStudio.R.Package.Repl.Data {
    internal sealed class ImportDataSetUrlCommand : PackageCommand {
        public ImportDataSetUrlCommand() :
            base(RGuidList.RCmdSetGuid, RPackageCommandId.icmdImportDatasetUrl) {
        }

        internal override void SetStatus() {
            this.Enabled = false; // ReplWindow.ReplWindowExists();
        }
    }
}
