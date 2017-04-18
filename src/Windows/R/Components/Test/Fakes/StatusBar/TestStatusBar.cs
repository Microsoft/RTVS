﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.Common.Core.Disposables;
using Microsoft.R.Components.StatusBar;
using Microsoft.UnitTests.Core.Mef;

namespace Microsoft.R.Components.Test.Fakes.StatusBar {
    [ExcludeFromCodeCoverage]
    [Export(typeof(IStatusBar))]
    [PartMetadata(PartMetadataAttributeNames.SkipInEditorTestCompositionCatalog, null)]
    public class TestStatusBar : IStatusBar {
        public IDisposable AddItem(UIElement item) => Disposable.Empty;
    }
}
