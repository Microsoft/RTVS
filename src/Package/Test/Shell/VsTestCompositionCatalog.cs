﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Editor.Test.Shell;

namespace Microsoft.VisualStudio.R.Package.Test.Shell {
    [ExcludeFromCodeCoverage]
    public sealed class VsTestCompositionCatalog {
        private static string[] _assemblies = new string[] {
            "Microsoft.VisualStudio.Shell.Mocks.dll",
            "Microsoft.VisualStudio.R.Package.dll",
            "Microsoft.VisualStudio.R.Package.Test.dll",
            "Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.dll",
        };
        public static ICompositionCatalog Current { get; } = new EditorTestCompositionCatalog(_assemblies);
    }
}
