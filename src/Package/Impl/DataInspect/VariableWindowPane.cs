﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.R.Package.DataInspect {
    [Guid("99d2ea62-72f2-33be-afc8-b8ce6e43b5d0")]
    public class VariableWindowPane : ToolWindowPane {
        public VariableWindowPane() {
            Caption = Resources.VariableWindowCaption;
            Content = new VariableView(RToolsSettings.Current);

            // this value matches with icmdShowVariableExplorerWindow's Icon in VSCT file
            BitmapImageMoniker = KnownMonikers.VariableProperty;
        }
    }
}
