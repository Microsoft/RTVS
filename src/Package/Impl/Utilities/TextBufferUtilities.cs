﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.R.Package.Utilities {
    public static class TextBufferUtilities {
        private static IVsEditorAdaptersFactoryService _adaptersFactoryService;

        public static IVsEditorAdaptersFactoryService AdaptersFactoryService {
            get {
                if (_adaptersFactoryService == null) {
                    _adaptersFactoryService = VsAppShell.Current.GetService<IVsEditorAdaptersFactoryService>();
                }
                return _adaptersFactoryService;
            }
            internal set {
                _adaptersFactoryService = value;
            }
        }

        public static T GetBufferAdapter<T>(this ITextBuffer textBuffer) where T : class {
            var vsTextBuffer = AdaptersFactoryService.GetBufferAdapter(textBuffer);
            if (vsTextBuffer == null) {
                var sp = VsAppShell.Current.GetService<IServiceProvider>();
                vsTextBuffer = AdaptersFactoryService.CreateVsTextBufferAdapterForSecondaryBuffer(sp, textBuffer);
            }
            return vsTextBuffer as T;
        }

        public static ITextBuffer ToITextBuffer(this IVsTextBuffer vsTextBuffer) => AdaptersFactoryService.GetDocumentBuffer(vsTextBuffer);

        public static ITextBuffer ToITextBuffer(this IVsTextLayer vsTextLayer) {
            IVsTextLines vsTextLines;
            vsTextLayer.GetBaseBuffer(out vsTextLines);

            return vsTextLines.ToITextBuffer();
        }

        public static ITextBuffer ToITextBuffer(this IVsTextLines vsTextLines) => ToITextBuffer(vsTextLines as IVsTextBuffer);
    }
}
