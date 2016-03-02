﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.R.Package.Utilities {
    public interface IActiveWpfTextViewTracker {
        IWpfTextView GetLastActiveTextView(IContentType contentType);
        IWpfTextView GetLastActiveTextView(string contentType);
    }
}