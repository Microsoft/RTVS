﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Common.Core.IO;
using Microsoft.R.Host.Client;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.R.Package.History;
using Microsoft.VisualStudio.R.Package.Repl;
using Microsoft.VisualStudio.R.Package.Shell;

namespace Microsoft.VisualStudio.R.Package.Test.Repl {
    [ExcludeFromCodeCoverage]
    public class ExportsTest {
        [Test]
        [Category.Repl]
        public void FileSystem_ExportTest() {
            Lazy<IFileSystem> lazy = VsAppShell.Current.ExportProvider.GetExport<IFileSystem>();
            lazy.Should().NotBeNull();
            lazy.Value.Should().NotBeNull();
        }

        [Test]
        [Category.Repl]
        public void RSessionProvider_ExportTest() {
            Lazy<IRSessionProvider> lazy = VsAppShell.Current.ExportProvider.GetExport<IRSessionProvider>();
            lazy.Should().NotBeNull();
            lazy.Value.Should().NotBeNull();
        }

        [Test]
        [Category.Repl]
        public void RHistoryProvider_ExportTest() {
            Lazy<IRHistoryProvider> provider = VsAppShell.Current.ExportProvider.GetExport<IRHistoryProvider>();
            provider.Should().NotBeNull();
            provider.Value.Should().NotBeNull();
        }

        [Test]
        [Category.Repl]
        public void RInteractiveProvider_ExportTest() {
            var provider = VsAppShell.Current.ExportProvider.GetExport<IRInteractiveProvider>();
            provider.Should().NotBeNull();
            provider.Value.Should().NotBeNull();
        }
    }
}
