﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using static System.FormattableString;
using Microsoft.R.Host.Client;

namespace Microsoft.R.Host.Client.Session {
    public static class RSessionEvaluationCommands {
        public static Task OptionsSetWidth(this IRExpressionEvaluator evaluation, int width) {
            return evaluation.EvaluateAsync($"options(width=as.integer({width}))\n", REvaluationKind.Mutating);
        }

        public static async Task<string> GetRUserDirectory(this IRExpressionEvaluator evaluation) {
            var result = await evaluation.EvaluateAsync("Sys.getenv('R_USER')", REvaluationKind.Normal);
            return result.StringResult.Replace('/', '\\');
        }

        public static async Task<string> GetWorkingDirectory(this IRExpressionEvaluator evaluation) {
            var result = await evaluation.EvaluateAsync("getwd()", REvaluationKind.Normal);
            return result.StringResult.Replace('/', '\\');
        }

        public static Task SetWorkingDirectory(this IRExpressionEvaluator evaluation, string path) {
            return evaluation.EvaluateAsync($"setwd('{path.Replace('\\', '/')}')\n", REvaluationKind.Normal);
        }

        public static Task SetDefaultWorkingDirectory(this IRExpressionEvaluator evaluation) {
            return evaluation.EvaluateAsync($"setwd('~')\n", REvaluationKind.Normal);
        }

        public static Task<REvaluationResult> LoadWorkspace(this IRExpressionEvaluator evaluation, string path) {
            return evaluation.EvaluateAsync($"load('{path.Replace('\\', '/')}', .GlobalEnv)\n", REvaluationKind.Mutating);
        }

        public static Task<REvaluationResult> SaveWorkspace(this IRExpressionEvaluator evaluation, string path) {
            return evaluation.EvaluateAsync($"save.image(file='{path.Replace('\\', '/')}')\n", REvaluationKind.Normal);
        }

        public static Task<REvaluationResult> SetVsGraphicsDevice(this IRExpressionEvaluator evaluation) {
            var script = @"
attach(as.environment(list(ide = function() { rtvs:::graphics.ide.new() })), name='rtvs::graphics::ide')
options(device='ide')
grDevices::deviceIsInteractive('ide')
";
            return evaluation.EvaluateAsync(script, REvaluationKind.Normal);
        }

        public static Task ResizePlot(this IRSessionInteraction evaluation, int width, int height) {
            var script = string.Format("rtvs:::graphics.ide.resize({0}, {1})\n", width, height);
            return evaluation.RespondAsync(script);
        }

        public static Task NextPlot(this IRSessionInteraction evaluation) {
            var script = "rtvs:::graphics.ide.nextplot()\n";
            return evaluation.RespondAsync(script);
        }

        public static Task PreviousPlot(this IRSessionInteraction evaluation) {
            var script = "rtvs:::graphics.ide.previousplot()\n";
            return evaluation.RespondAsync(script);
        }

        public static Task ClearPlotHistory(this IRSessionInteraction evaluation) {
            var script = "rtvs:::graphics.ide.clearplots()\n";
            return evaluation.RespondAsync(script);
        }

        public static Task RemoveCurrentPlot(this IRSessionInteraction evaluation) {
            var script = "rtvs:::graphics.ide.removeplot()\n";
            return evaluation.RespondAsync(script);
        }

        public static Task<REvaluationResult> PlotHistoryInfo(this IRExpressionEvaluator evaluation) {
            var script = @"rtvs:::graphics.ide.historyinfo()";
            return evaluation.EvaluateAsync(script, REvaluationKind.Json);
        }

        public static Task InstallPackage(this IRSessionInteraction interaction, string name) {
            var script = $"install.packages({name.ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }
        
        public static Task InstallPackage(this IRSessionInteraction interaction, string name, string libraryPath) {
            var script = $"install.packages({name.ToRStringLiteral()}, lib={libraryPath.ToRPath().ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }
        
        public static Task UninstallPackage(this IRSessionInteraction interaction, string name) {
            var script = $"remove.packages({name.ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }

        public static Task UninstallPackage(this IRSessionInteraction interaction, string name, string libraryPath) {
            var script = $"remove.packages({name.ToRStringLiteral()}, lib={libraryPath.ToRPath().ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }

        public static Task LoadPackage(this IRSessionInteraction interaction, string name) {
            var script = $"library({name.ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }

        public static Task LoadPackage(this IRSessionInteraction interaction, string name, string libraryPath) {
            var script = $"library({name.ToRStringLiteral()}, lib.loc={libraryPath.ToRPath().ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }

        public static Task UnloadPackage(this IRSessionInteraction interaction, string name) {
            var script = $"unloadNamespace({name.ToRStringLiteral()})\n";
            return interaction.RespondAsync(script);
        }

        public static Task<REvaluationResult> InstalledPackages(this IRExpressionEvaluator evaluation) {
            var script = @"rtvs:::packages.installed()";
            return evaluation.EvaluateAsync(script, REvaluationKind.Json);
        }
        
        public static Task<REvaluationResult> AvailablePackages(this IRExpressionEvaluator evaluation) {
            var script = @"rtvs:::packages.available()";
            return evaluation.EvaluateAsync(script, REvaluationKind.Json | REvaluationKind.Reentrant);
        }

        public static Task<REvaluationResult> LoadedPackages(this IRExpressionEvaluator evaluation) {
            var script = @"rtvs:::packages.loaded()";
            return evaluation.EvaluateAsync(script, REvaluationKind.Json);
        }

        public static Task<REvaluationResult> LibraryPaths(this IRExpressionEvaluator evaluation) {
            var script = @"rtvs:::packages.libpaths()";
            return evaluation.EvaluateAsync(script, REvaluationKind.Json);
        }

        public static Task<REvaluationResult> ExportToBitmap(this IRExpressionEvaluator evaluation, string deviceName, string outputFilePath, int widthInPixels, int heightInPixels) {
            string script = string.Format("rtvs:::graphics.ide.exportimage(\"{0}\", {1}, {2}, {3})", outputFilePath.Replace("\\", "/"), deviceName, widthInPixels, heightInPixels);
            return evaluation.EvaluateAsync(script, REvaluationKind.Normal);
        }

        public static Task<REvaluationResult> ExportToMetafile(this IRExpressionEvaluator evaluation, string outputFilePath, double widthInInches, double heightInInches) {
            string script = string.Format("rtvs:::graphics.ide.exportimage(\"{0}\", win.metafile, {1}, {2})", outputFilePath.Replace("\\", "/"), widthInInches, heightInInches);
            return evaluation.EvaluateAsync(script, REvaluationKind.Normal);
        }

        public static Task<REvaluationResult> ExportToPdf(this IRExpressionEvaluator evaluation, string outputFilePath, double widthInInches, double heightInInches) {
            string script = string.Format("rtvs:::graphics.ide.exportpdf(\"{0}\", {1}, {2})", outputFilePath.Replace("\\", "/"), widthInInches, heightInInches);
            return evaluation.EvaluateAsync(script, REvaluationKind.Normal);
        }

        public static async Task SetVsCranSelection(this IRExpressionEvaluator evaluation, string mirrorUrl) {
            await evaluation.EvaluateAsync(Invariant($"rtvs:::set_mirror({mirrorUrl.ToRStringLiteral()})"), REvaluationKind.Mutating);
        }

        public static Task<REvaluationResult> SetVsHelpRedirection(this IRExpressionEvaluator evaluation) {
            var script =
@"options(help_type = 'html')
  options(browser = function(url) { 
      rtvs:::send_message('Browser', url) 
  })";
            return evaluation.EvaluateAsync(script, REvaluationKind.Mutating);
        }

        public static Task<REvaluationResult> SetChangeDirectoryRedirection(this IRExpressionEvaluator evaluation) {
            var script =
@"utils::assignInNamespace('setwd', function(dir) {
    old <- .Internal(setwd(dir))
    rtvs:::send_message('setwd', dir)
    invisible(old)
  }, 'base')";
            return evaluation.EvaluateAsync(script, REvaluationKind.Mutating);
        }
    }
}