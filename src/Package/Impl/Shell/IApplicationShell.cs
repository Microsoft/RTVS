﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Languages.Editor.Shell;

namespace Microsoft.VisualStudio.R.Package.Shell {
    /// <summary>
    /// Application shell provides access to services such as 
    /// composition container, export provider, global VS IDE
    /// services and so on.
    /// </summary>
    public interface IApplicationShell : IEditorShell {
        string BrowseForFileOpen(IntPtr owner, string filter, string initialPath = null, string title = null);

        string BrowseForFileSave(IntPtr owner, string filter, string initialPath = null, string title = null);
    }
}
