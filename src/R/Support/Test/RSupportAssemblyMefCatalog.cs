﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Languages.Editor.Test;

namespace Microsoft.R.Support.Test {
    // Fixture doesn't import itself. Use AssemblyFixtureImportAttribute
    [ExcludeFromCodeCoverage]
    public class RSupportAssemblyMefCatalog : EditorAssemblyMefCatalog {
        protected override IEnumerable<string> GetAssemblies() => base.GetAssemblies().Concat(new[] {
            "Microsoft.VisualStudio.InteractiveWindow.dll",
            "Microsoft.R.Support.dll",
            "Microsoft.R.Support.Test.dll",
            "System.Collections.Immutable.dll"
        });
    }
}
