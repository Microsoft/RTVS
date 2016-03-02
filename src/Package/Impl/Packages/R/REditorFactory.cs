﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.R.Package.Editors;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.R.Packages.R {
    [Guid(RGuidList.REditorFactoryGuidString)]
    internal sealed class REditorFactory : BaseEditorFactory {
        public REditorFactory(Microsoft.VisualStudio.Shell.Package package) :
            base(package, RGuidList.REditorFactoryGuid, RGuidList.RLanguageServiceGuid) { }

        public override int CreateEditorInstance(
            uint createEditorFlags,
            string documentMoniker,
            string physicalView,
            IVsHierarchy hierarchy,
            uint itemid,
            IntPtr docDataExisting,
            out IntPtr docView,
            out IntPtr docData,
            out string editorCaption,
            out Guid commandUIGuid,
            out int createDocumentWindowFlags) {
            return base.CreateEditorInstance(
                createEditorFlags,
                documentMoniker,
                physicalView,
                hierarchy,
                itemid,
                docDataExisting,
                out docView,
                out docData,
                out editorCaption,
                out commandUIGuid,
                out createDocumentWindowFlags);
        }
    }
}
