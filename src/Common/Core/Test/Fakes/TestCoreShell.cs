﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core.Shell;
using Microsoft.UnitTests.Core.Threading;

namespace Microsoft.Common.Core.Test.Fakes {
    public sealed class TestCoreShell : ICoreShell {
        private readonly CompositionContainer _container;

        public TestCoreShell(CompositionContainer container) {
            _container = container;
        }

        public ExportProvider ExportProvider => _container;
        public ICompositionService CompositionService => _container;

        public void DispatchOnUIThread(Action action) {
            UIThreadHelper.Instance.Invoke(action);
        }
        
        public async Task DispatchOnMainThreadAsync(Action action, CancellationToken cancellationToken = default(CancellationToken)) {
            await UIThreadHelper.Instance.InvokeAsync(action);
        }

        public Task<TResult> DispatchOnMainThreadAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken = default(CancellationToken)) {
            return UIThreadHelper.Instance.InvokeAsync(callback);
        }

        public Thread MainThread => UIThreadHelper.Instance.Thread;

#pragma warning disable 67
        public event EventHandler<EventArgs> Idle;
        public event EventHandler<EventArgs> Terminating;

        public void ShowErrorMessage(string message) {
            LastShownErrorMessage = message;
        }

        public void ShowContextMenu(CommandID commandId, int x, int y) {
            LastShownContextMenu = commandId;
        }

        public MessageButtons ShowMessage(string message, MessageButtons buttons) {
            LastShownMessage = message;
            return MessageButtons.OK;
        }

        public int LocaleId => 1033;

        public string LastShownMessage { get; private set; }
        public string LastShownErrorMessage { get; private set; }
        public CommandID LastShownContextMenu { get; private set; }
    }
}