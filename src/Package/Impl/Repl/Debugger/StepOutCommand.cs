﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.R.Package.Commands;

namespace Microsoft.VisualStudio.R.Package.Repl.Debugger {
    internal sealed class StepOutCommand : DebuggerWrappedCommand {
        public StepOutCommand(IRSessionProvider rSessionProvider)
            : base(rSessionProvider, RPackageCommandId.icmdStepOut, 
                   VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.StepOut,
                   DebuggerCommandVisibility.Stopped) {
        }

        internal override void SetStatus() {
            base.SetStatus();
            Enabled = false;
        }
    }
}
