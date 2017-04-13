﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Languages.Editor.Test;

namespace Microsoft.R.Editor.Test {
    [ExcludeFromCodeCoverage]
    public class REditorAssemblyMefCatalog : EditorAssemblyMefCatalog {
        protected override IEnumerable<string> GetAssemblies() => base.GetAssemblies().Concat(new[] {
            "Microsoft.R.Editor",
            "Microsoft.R.Editor.Test"
        });
    }
}
