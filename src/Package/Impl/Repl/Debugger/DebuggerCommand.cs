﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.R.Package.Repl.Debugger {
    internal abstract class DebuggerCommand : PackageCommand {
        protected readonly IRSession RSession;
        private readonly DebuggerCommandVisibility _visibility;

        protected DebuggerCommand(IRSessionProvider rSessionProvider, int cmdId, DebuggerCommandVisibility visibility)
            : base(RGuidList.RCmdSetGuid, cmdId) {
            RSession = rSessionProvider.GetInteractiveWindowRSession();
            _visibility = visibility;
        }

        internal override void SetStatus() {
            Enabled = false;
            Visible = false;

            if (!RSession.IsHostRunning) {
                return;
            }

            var debugger = VsAppShell.Current.GetGlobalService<IVsDebugger>(typeof(SVsShellDebugger));
            if (debugger == null) {
                return;
            }

            var mode = new DBGMODE[1];
            if (debugger.GetMode(mode) < 0) {
                return;
            }

            if (mode[0] == DBGMODE.DBGMODE_Design) {
                if (_visibility == DebuggerCommandVisibility.DesignMode) {
                    Visible = ReplWindow.Current.IsActive;
                    Enabled = true;
                }
                return;
            }

            if ((_visibility & DebuggerCommandVisibility.DebugMode) > 0) {
                Visible = ReplWindow.Current.IsActive;

                if (mode[0] == DBGMODE.DBGMODE_Break) {
                    Enabled = (_visibility & DebuggerCommandVisibility.Stopped) > 0;
                    return;
                }
                if (mode[0] == DBGMODE.DBGMODE_Run) {
                    Enabled = (_visibility & DebuggerCommandVisibility.Run) > 0;
                    return;
                }
            }
        }
    }
}
