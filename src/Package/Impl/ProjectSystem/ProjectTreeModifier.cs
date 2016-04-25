﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#if VS14
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.R.Package.ProjectSystem {
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo("RTools")]
    internal sealed class ProjectTreeModifier : IProjectTreeModifier {
        public IProjectTree ApplyModifications(IProjectTree tree, IProjectTreeProvider projectTreeProvider) {
            if (tree != null) {
                if (tree.Capabilities.Contains(ProjectTreeCapabilities.ProjectRoot)) {
                    tree = tree.SetIcon(ProjectIconProvider.ProjectNodeImage.ToProjectSystemType());
                } else if (tree.Capabilities.Contains(ProjectTreeCapabilities.FileOnDisk)) {
                    string ext = Path.GetExtension(tree.FilePath).ToLowerInvariant();
                    if (ext == ".r") {
                        tree = tree.SetIcon(ProjectIconProvider.RFileNodeImage.ToProjectSystemType());
                    } else if (ext == ".rdata" || ext == ".rhistory") {
                        tree = tree.SetIcon(ProjectIconProvider.RDataFileNodeImage.ToProjectSystemType());
                    }
                    if (ext == ".md" || ext == ".rmd") {
                        tree = tree.SetIcon(KnownMonikers.MarkdownFile.ToProjectSystemType());
                    }
                }
            }
            return tree;
        }
    }
}
#endif
