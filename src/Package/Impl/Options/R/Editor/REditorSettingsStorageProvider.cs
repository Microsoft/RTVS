﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Editor.Settings;
using Microsoft.R.Components.ContentTypes;
using Microsoft.VisualStudio.R.Package.Editors;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.R.Package.Options.R.Editor {
    [Export(typeof(IEditorSettingsStorageProvider))]
    [ContentType(RContentTypeDefinition.ContentType)]
    internal sealed class REditorSettingsStorageProvider : IEditorSettingsStorageProvider {
        private readonly ICoreShell _coreShell;

        [ImportingConstructor]
        public REditorSettingsStorageProvider(ICoreShell coreShell) {
            _coreShell = coreShell;
        }

        public IEditorSettingsStorage GetSettingsStorage()
            => new LanguageSettingsStorage(_coreShell, RGuidList.RLanguageServiceGuid, RGuidList.RPackageGuid, new string[] { RPackage.ProductName });
    }
}
