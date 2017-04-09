﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core.UI;
using Microsoft.UnitTests.Core.Threading;

namespace Microsoft.Common.Core.Test.Fakes.Shell {
    [ExcludeFromCodeCoverage]
    public class TestProgressDialog : IProgressDialog {
        public void Show(Func<CancellationToken, Task> method, string waitMessage, int delayToShowDialogMs = 0)
            => UIThreadHelper.Instance.BlockUntilCompleted(() => method(CancellationToken.None));

        public TResult Show<TResult>(Func<CancellationToken, Task<TResult>> method, string waitMessage, int delayToShowDialogMs = 0)
            => UIThreadHelper.Instance.BlockUntilCompleted(() => method(CancellationToken.None));

        public void Show(Func<IProgress<ProgressDialogData>, CancellationToken, Task> method, string waitMessage, int totalSteps = 100, int delayToShowDialogMs = 0)
            => UIThreadHelper.Instance.BlockUntilCompleted(() => method(new Progress<ProgressDialogData>(), CancellationToken.None));

        public T Show<T>(Func<IProgress<ProgressDialogData>, CancellationToken, Task<T>> method, string waitMessage, int totalSteps = 100, int delayToShowDialogMs = 0)
            => UIThreadHelper.Instance.BlockUntilCompleted(() => method(new Progress<ProgressDialogData>(), CancellationToken.None));
    }
}