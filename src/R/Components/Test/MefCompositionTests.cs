﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.R.Components.History;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.PackageManager;
using Microsoft.R.Components.Search;
using Microsoft.R.InteractiveWindow;
using Microsoft.UnitTests.Core.Mef;
using Microsoft.UnitTests.Core.XUnit;

namespace Microsoft.R.Components.Test {
    /// <summary>
    /// These tests are basic markers that all required composition imports are available.
    /// </summary>
    public class MefCompositionTests : IDisposable {
        private readonly IExportProvider _exportProvider;

        public MefCompositionTests(RComponentsMefCatalogFixture mefCatalog) {
            _exportProvider = mefCatalog.CreateExportProvider();
        }

        public void Dispose() {
            _exportProvider.Dispose();
        }

        [Test]
        public void SearchControlProvider() {
            _exportProvider.GetExportedValue<ISearchControlProvider>().Should().NotBeNull();
        }

        [Test]
        public void RHistoryProvider() {
            _exportProvider.GetExportedValue<IRHistoryProvider>().Should().NotBeNull();
        }

        [Test]
        public void RInteractiveWorkflowProvider() {
            _exportProvider.GetExportedValue<IRInteractiveWorkflowProvider>().Should().NotBeNull();
        }

        [Test]
        public void RHistoryVisualComponentContainerFactory() {
            _exportProvider.GetExportedValue<IRHistoryVisualComponentContainerFactory>().Should().NotBeNull();
        }

        [Test]
        public void InteractiveWindowComponentContainerFactory() {
            _exportProvider.GetExportedValue<IInteractiveWindowComponentContainerFactory>().Should().NotBeNull();
        }

        [Test]
        public void RPackageManagerVisualComponentContainerFactory() {
            _exportProvider.GetExportedValue<IRPackageManagerVisualComponentContainerFactory>().Should().NotBeNull();
        }

        [Test]
        public void InteractiveWindowFactoryService() {
            _exportProvider.GetExportedValue<IInteractiveWindowFactoryService>().Should().NotBeNull();
        }
    }
}
