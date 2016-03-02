﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Definitions;
using Microsoft.VisualStudio.R.Package.Interop;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.R.Package.Help {
    [Guid(WindowGuid)]
    internal class HelpWindowPane : ToolWindowPane, IVisualComponentContainer<IHelpWindowVisualComponent> {
        internal const string WindowGuid = "9E909526-A616-43B2-A82B-FD639DCD40CB";
        private IHelpWindowVisualComponent _component;

        public HelpWindowPane() {

            Caption = Resources.HelpWindowCaption;
            BitmapImageMoniker = KnownMonikers.StatusHelp;

            _component = new HelpWindowVisualComponent();
            Content = _component.Control;

            this.ToolBar = new CommandID(RGuidList.RCmdSetGuid, RPackageCommandId.helpWindowToolBarId);
            this.ToolBarCommandTarget = new CommandTargetToOleShim(null, _component.Controller);
        }

        public IHelpWindowVisualComponent Component => _component;

        protected override void Dispose(bool disposing) {
            if (disposing && _component != null) {
                _component.Dispose();
                _component = null;
            }
            base.Dispose(disposing);
        }
    }
}
