﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Editor.Outline;
using Microsoft.R.Editor.Document;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.R.Editor.Outline {
    internal sealed class ROutliningTagger : OutliningTagger {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public ROutliningTagger(REditorDocument document, ICoreShell shell)
            : base(document.EditorTree.TextBuffer, new ROutlineRegionBuilder(document, shell)) {
            document.DocumentClosing += OnDocumentClosing;

            ServiceManager.AddService<ROutliningTagger>(this, document.EditorTree.TextBuffer, shell);
        }

        private void OnDocumentClosing(object sender, EventArgs e) {
            REditorDocument document = (REditorDocument)sender;
            document.DocumentClosing -= OnDocumentClosing;

            ServiceManager.RemoveService<ROutliningTagger>(document.EditorTree.TextBuffer);
        }

        public override OutliningRegionTag CreateTag(OutlineRegion region) => new ROutliningRegionTag(region);
    }
}