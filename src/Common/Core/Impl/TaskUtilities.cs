﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Common.Core {
    public static class TaskUtilities {
        public static bool IsOnBackgroundThread() {
            var taskScheduler = TaskScheduler.Current;
            var syncContext = SynchronizationContext.Current;
            return taskScheduler == TaskScheduler.Default && (syncContext == null || syncContext.GetType() == typeof(SynchronizationContext));
        }

        /// <summary>
        /// If awaited on a thread with custom scheduler or synchronization context, invokes the continuation
        /// on a background (thread pool) thread. If already on such a thread, await is a no-op.
        /// </summary>
        public static BackgroundThreadAwaitable SwitchToBackgroundThread() {
            return new BackgroundThreadAwaitable();
        }

        public struct BackgroundThreadAwaitable {
            public Awaiter GetAwaiter() {
                return new Awaiter();
            }

            public struct Awaiter : ICriticalNotifyCompletion {
                private static readonly WaitCallback WaitCallback = state => ((Action)state)();

                public bool IsCompleted => IsOnBackgroundThread();

                public void OnCompleted(Action continuation) {
                    Trace.Assert(continuation != null);
                    ThreadPool.QueueUserWorkItem(WaitCallback, continuation);
                }

                public void UnsafeOnCompleted(Action continuation) {
                    Trace.Assert(continuation != null);
                    ThreadPool.UnsafeQueueUserWorkItem(WaitCallback, continuation);
                }

                public void GetResult() {
                }
            }
        }

        [Conditional("TRACE")]
        public static void AssertIsOnBackgroundThread(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
        ) {
            if (!IsOnBackgroundThread()) {
                Trace.Fail($"{memberName} at {sourceFilePath}:{sourceLineNumber} was incorrectly called from a non-background thread.");
            }
        }
    }
}
