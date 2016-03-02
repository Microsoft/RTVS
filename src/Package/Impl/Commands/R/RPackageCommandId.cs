﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.VisualStudio.R.Package.Commands {
    public static class RPackageCommandId {
        public const int plotWindowToolBarId = 0x2000;
        public const int helpWindowToolBarId = 0x2010;
        public const int historyWindowToolBarId = 0x2020;

        // General
        public const int icmdGoToFormattingOptions = 400;
        public const int icmdGoToRToolsOptions = 401;
        public const int icmdGoToREditorOptions = 402;
        public const int icmdSendSmile = 403;
        public const int icmdSendFrown = 404;
        public const int icmdImportRSettings = 405;

        // REPL
        public const int icmdLoadWorkspace = 502;
        public const int icmdSaveWorkspace = 503;
        public const int icmdSelectWorkingDirectory = 504;
        public const int icmdResetRepl = 505;
        public const int icmdInterruptR = 506;
        public const int icmdAttachDebugger = 507;
        public const int icmdSourceRScript = 508;
        public const int icmdGetDirectoryList = 509;
        public const int icmdSetWorkingDirectory = 510;
        public const int icmdContinueDebugging = 511;
        public const int icmdStopDebugging = 512;
        public const int icmdStepInto = 513;
        public const int icmdStepOut = 514;
        public const int icmdStepOver = 515;
        public const int icmdAttachToRInteractive = 516;

        public const int icmdRexecuteReplCmd = 571;
        public const int icmdPasteReplCmd = 572;

        // Packages
        public const int icmdInstallPackages = 601;
        public const int icmdCheckForPackageUpdates = 602;

        // Plots
        public const int icmdRemovePlot = 705;
        public const int icmdNextPlot = 710;
        public const int icmdPrevPlot = 711;
        public const int icmdClearPlots = 712;
        public const int icmdExportPlotAsImage = 713;
        public const int icmdExportPlotAsPdf = 714;
        public const int icmdCopyPlotAsBitmap = 716;
        public const int icmdCopyPlotAsMetafile = 717;

        // Data
        public const int icmdImportDataset = 801;
        public const int icmdImportDatasetUrl = 802;
        public const int icmdImportDatasetTextFile = 803;

        // Window management
        public const int icmdShowReplWindow = 901;
        public const int icmdShowPlotWindow = 902;
        public const int icmdShowVariableExplorerWindow = 903;
        public const int icmdShowHistoryWindow = 904;
        public const int icmdShowPackagesWindow = 905;
        public const int icmdShowHelpWindow = 906;

        // Publishing
        //public const int icmdPublishDialog = 1001;
        //public const int icmdPublishPreviewHtml = 1002;
        //public const int icmdPublishPreviewPdf = 1003;
        //public const int icmdPublishPreviewWord = 1004;

        // Help
        public const int icmdHelpPrevious = 1100;
        public const int icmdHelpNext = 1101;
        public const int icmdHelpHome = 1102;
        public const int icmdHelpRefresh = 1103;
        public const int icmdHelpOnCurrent = 1104;

        // History
        public const int icmdLoadHistory = 1200;
        public const int icmdSaveHistory = 1201;
        public const int icmdSendHistoryToRepl = 1202;
        public const int icmdSendHistoryToSource = 1203;
        public const int icmdDeleteSelectedHistoryEntries = 1204;
        public const int icmdDeleteAllHistoryEntries = 1205;
        public const int icmdToggleMultilineSelection = 1206;

        // Debugger
        public const int icmdShowDotPrefixedVariables = 1300;

        // Documentation
        public const int icmdRtvsDocumentation = 1400;
        public const int icmdRtvsSamples = 1401;
        public const int icmdRDocsIntroToR = 1402;
        public const int icmdRDocsTaskViews = 1403;
        public const int icmdRDocsDataImportExport = 1404;
        public const int icmdRDocsWritingRExtensions = 1405;
        public const int icmdMicrosoftRProducts = 1406;
        public const int icmdCheckForUpdates = 1407;
    }
}
