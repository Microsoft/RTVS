﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.UnitTests.Core.XUnit;

namespace Microsoft.R.Host.Client.Test {
    [AssemblyFixture]
    [ExcludeFromCodeCoverage]
    public class GraphicsDeviceTestFilesFixture : DeployFilesFixture {
        public string HistoryInfoResultPath { get; }
        public string ClearPlotsResultPath { get; }
        public string RemovePlotFirstResultPath { get; }
        public string RemovePlotLastResultPath { get; }
        public string RemovePlotMiddleResultPath { get; }
        public string RemovePlotSingleResultPath { get; }
        public string ExportToPdfResultPath { get; }
        public string ExportToBmpResultPath { get; }
        public string ExportToPngResultPath { get; }
        public string ExportToJpegResultPath { get; }
        public string ExportToTiffResultPath { get; }
        public string ExportPreviousPlotToImageResultPath { get; }
        public string ExpectedExportPreviousPlotToImagePath { get; }
        public string ExpectedExportToPdfPath { get; }
        public string ActualFolderPath { get; }

        public GraphicsDeviceTestFilesFixture() : base(@"Host\Client\Test\Files", "Files") {
            ActualFolderPath = Path.Combine(DestinationPath, "Actual");
            Directory.CreateDirectory(ActualFolderPath);

            // Path to files that are generated when tests are executed
            HistoryInfoResultPath = Path.Combine(ActualFolderPath, "HistoryInfoResult.json");
            ClearPlotsResultPath = Path.Combine(ActualFolderPath, "ClearPlotsResult.json");
            RemovePlotFirstResultPath = Path.Combine(ActualFolderPath, "RemovePlotFirstResult.json");
            RemovePlotLastResultPath = Path.Combine(ActualFolderPath, "RemovePlotLastResult.json");
            RemovePlotMiddleResultPath = Path.Combine(ActualFolderPath, "RemovePlotMiddleResult.json");
            RemovePlotSingleResultPath = Path.Combine(ActualFolderPath, "RemovePlotSingleResult.json");
            ExportToPdfResultPath = Path.Combine(ActualFolderPath, "ExportToPdfResult.pdf");
            ExportToBmpResultPath = Path.Combine(ActualFolderPath, "ExportToBmpResult.bmp");
            ExportToPngResultPath = Path.Combine(ActualFolderPath, "ExportToPngResult.png");
            ExportToJpegResultPath = Path.Combine(ActualFolderPath, "ExportToJpegResult.jpg");
            ExportToTiffResultPath = Path.Combine(ActualFolderPath, "ExportToTiffResult.tif");
            ExportPreviousPlotToImageResultPath = Path.Combine(ActualFolderPath, "ExportPreviousPlotToImageResultPath.bmp");

            // Path to files that are compared against and are included as part of test sources
            ExpectedExportPreviousPlotToImagePath = Path.Combine(DestinationPath, "ExportPreviousPlotToImageExpectedResult.bmp");
            ExpectedExportToPdfPath = Path.Combine(DestinationPath, "ExportToPdfExpectedResult.pdf");
        }
    }
}