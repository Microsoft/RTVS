﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.Settings;
using Microsoft.R.InteractiveWindow;
using Microsoft.UnitTests.Core.Mef;
using Microsoft.UnitTests.Core.Threading;
using Microsoft.UnitTests.Core.XUnit;

namespace Microsoft.R.Components.Test.InteractiveWorkflow {
    public class RInteractiveEvaluatorTest : IDisposable {
        private readonly IExportProvider _exportProvider;
        private readonly IRInteractiveWorkflowProvider _workflowProvider;
        private readonly IInteractiveWindowComponentContainerFactory _interactiveWindowComponentContainerFactory;

        public RInteractiveEvaluatorTest(RComponentsMefCatalogFixture catalog) {
            _exportProvider = catalog.CreateExportProvider();

            var settings = _exportProvider.GetExportedValue<IRSettings>();
            settings.RCodePage = 1252;

            _workflowProvider = _exportProvider.GetExportedValue<IRInteractiveWorkflowProvider>();
            _interactiveWindowComponentContainerFactory = _exportProvider.GetExportedValue<IInteractiveWindowComponentContainerFactory>();
        }

        public void Dispose() {
            (_exportProvider as IDisposable)?.Dispose();
        }

        [Test]
        [Category.Interactive]
        public async Task EvaluatorTest01() {
            var workflow = _workflowProvider.GetOrCreate();
            var session = workflow.RSession;
            using (await UIThreadHelper.Instance.Invoke(() => workflow.GetOrCreateVisualComponent(_interactiveWindowComponentContainerFactory))) {
                workflow.ActiveWindow.Should().NotBeNull();
                session.IsHostRunning.Should().BeTrue();

                var window = workflow.ActiveWindow.InteractiveWindow;
                var eval = window.Evaluator;

                eval.CanExecuteCode("x <-").Should().BeFalse();
                eval.CanExecuteCode("(()").Should().BeFalse();
                eval.CanExecuteCode("a *(b+c)").Should().BeTrue();

                window.Operations.ClearView();
                var result = await eval.ExecuteCodeAsync(new string(new char[10000]) + "\r\n");
                result.Should().Be(ExecutionResult.Failure);
                window.FlushOutput();
                string text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.Should().Contain(string.Format(Microsoft.R.Components.Resources.InputIsTooLong, 4096));

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("z <- '電話帳 全米のお'\n");
                result.Should().Be(ExecutionResult.Success);

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("z" + Environment.NewLine);
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.TrimEnd().Should().Be("[1] \"電話帳 全米のお\"");

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("Encoding(z)\n");
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.TrimEnd().Should().Be("[1] \"UTF-8\"");

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("x <- c(1:10)\n");
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.Should().Be(string.Empty);

                window.Operations.ClearView();
                await eval.ResetAsync(initialize: false);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.Should().StartWith(Microsoft.R.Components.Resources.MicrosoftRHostStopping);
            }
        }

        [Test]
        [Category.Interactive]
        public async Task EvaluatorTest02() {
            var workflow = _workflowProvider.GetOrCreate();
            var session = workflow.RSession;
            using (await UIThreadHelper.Instance.Invoke(() => workflow.GetOrCreateVisualComponent(_interactiveWindowComponentContainerFactory))) {
                workflow.ActiveWindow.Should().NotBeNull();
                session.IsHostRunning.Should().BeTrue();

                var window = workflow.ActiveWindow.InteractiveWindow;
                var eval = window.Evaluator;

                window.Operations.ClearView();
                var result = await eval.ExecuteCodeAsync("w <- dQuote('text')" + Environment.NewLine);
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                var text = window.OutputBuffer.CurrentSnapshot.GetText();

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("w" + Environment.NewLine);
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.TrimEnd().Should().Be("[1] \"“text”\"");

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("e <- dQuote('абвг')" + Environment.NewLine);
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.Should().Be(string.Empty);

                window.Operations.ClearView();
                result = await eval.ExecuteCodeAsync("e" + Environment.NewLine);
                result.Should().Be(ExecutionResult.Success);
                window.FlushOutput();
                text = window.OutputBuffer.CurrentSnapshot.GetText();
                text.TrimEnd().Should().Be("[1] \"“абвг”\"");
            }
        }
    }
}
