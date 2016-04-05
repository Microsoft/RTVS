﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.R.Host.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace Microsoft.R.Debugger {
    public sealed class DebugSession : IDisposable {
        private Task _initializeTask;
        private readonly object _initializeLock = new object();

        private CancellationTokenSource _initialPromptCts = new CancellationTokenSource();
        private TaskCompletionSource<bool> _stepTcs;
        private DebugStackFrame _bpHitFrame;
        private volatile EventHandler<DebugBrowseEventArgs> _browse;
        private volatile DebugBrowseEventArgs _currentBrowseEventArgs;
        private readonly object _browseLock = new object();

        private Dictionary<DebugBreakpointLocation, DebugBreakpoint> _breakpoints = new Dictionary<DebugBreakpointLocation, DebugBreakpoint>();

        public IReadOnlyCollection<DebugBreakpoint> Breakpoints => _breakpoints.Values;

        public IRSession RSession { get; private set; }

        public bool IsBrowsing => _currentBrowseEventArgs != null;

        public event EventHandler<DebugBrowseEventArgs> Browse {
            add {
                var eventArgs = _currentBrowseEventArgs;
                if (eventArgs != null) {
                    value?.Invoke(this, eventArgs);
                }

                lock (_browseLock) {
                    _browse += value;
                }
            }
            remove {
                lock (_browseLock) {
                    _browse -= value;
                }
            }
        }

        public DebugSession(IRSession session) {
            RSession = session;
            RSession.Connected += RSession_Connected;
            RSession.BeforeRequest += RSession_BeforeRequest;
            RSession.AfterRequest += RSession_AfterRequest;
        }

        public void Dispose() {
            RSession.Connected -= RSession_Connected;
            RSession.BeforeRequest -= RSession_BeforeRequest;
            RSession.AfterRequest -= RSession_AfterRequest;
            RSession = null;
        }

        private void ThrowIfDisposed() {
            if (RSession == null) {
                throw new ObjectDisposedException(nameof(DebugSession));
            }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            lock (_initializeLock) {
                if (_initializeTask == null) {
                    _initializeTask = InitializeWorkerAsync(cancellationToken);
                }

                return _initializeTask;
            }
        }

        private async Task InitializeWorkerAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            ThrowIfDisposed();
            await TaskUtilities.SwitchToBackgroundThread();
            try {
                using (var eval = await RSession.BeginEvaluationAsync(cancellationToken: cancellationToken)) {
                    // Re-initialize the breakpoint table.
                    foreach (var bp in _breakpoints.Values) {
                        await bp.ReapplyBreakpointAsync(cancellationToken);
                    }

                    await eval.EvaluateAsync("rtvs:::reapply_breakpoints()", REvaluationKind.Mutating); // TODO: mark all breakpoints as invalid if this fails.
                }

                // Attach might happen when session is already at the Browse prompt, in which case we have
                // missed the corresponding BeginRequest event, but we want to raise Browse anyway. So
                // grab an interaction and check the prompt.
                RSession.BeginInteractionAsync(cancellationToken: cancellationToken).ContinueWith(async t => {
                    using (var inter = await t) {
                        // If we got AfterRequest before we got here, then that has already taken care of
                        // the prompt; or if it's not a Browse prompt, will do so in a future event. Bail out.'
                        if (_initialPromptCts.IsCancellationRequested) {
                            return;
                        }

                        // Otherwise, treat it the same as if AfterRequest just happened.
                        ProcessBrowsePrompt(inter.Contexts);
                    }
                }, cancellationToken).DoNotWait();
            } catch (Exception ex) when (!ex.IsCriticalException()) {
                Dispose();
                throw;
            }
        }

        public async Task<bool> ExecuteBrowserCommandAsync(string command, Func<IRSessionInteraction, Task<bool>> prepare = null, CancellationToken cancellationToken = default(CancellationToken)) {
            ThrowIfDisposed();
            await TaskUtilities.SwitchToBackgroundThread();

            using (var inter = await RSession.BeginInteractionAsync(isVisible: true, cancellationToken: cancellationToken)) {
                if (prepare != null) {
                    if (!await prepare(inter)) {
                        return false;
                    }
                }

                if (inter.Contexts.IsBrowser()) {
                    await inter.RespondAsync(command + "\n");
                    return true;
                }
            }

            return false;
        }

        internal async Task<REvaluationResult> InvokeDebugHelperAsync(string expression, CancellationToken cancellationToken, bool json = false) {
            TaskUtilities.AssertIsOnBackgroundThread();
            ThrowIfDisposed();

            REvaluationResult res;
            using (var eval = await RSession.BeginEvaluationAsync(cancellationToken)) {
                res = await eval.EvaluateAsync(expression, json ? REvaluationKind.Json : REvaluationKind.Normal);
                if (res.ParseStatus != RParseStatus.OK || res.Error != null || (json && res.JsonResult == null)) {
                    Trace.Fail(Invariant($"Internal debugger evaluation {expression} failed: {res}"));
                    throw new REvaluationException(res);
                }
            }

            return res;
        }

        internal async Task<TToken> InvokeDebugHelperAsync<TToken>(string expression, CancellationToken cancellationToken)
            where TToken : JToken {

            var res = await InvokeDebugHelperAsync(expression, cancellationToken, json: true);

            var token = res.JsonResult as TToken;
            if (token == null) {
                var err = Invariant($"Expected to receive {typeof(TToken).Name} in response to {expression}, but got {res.JsonResult?.GetType().Name}");
                Trace.Fail(err);
                throw new JsonException(err);
            }

            return token;
        }
        public Task<DebugEvaluationResult> EvaluateAsync(
            string expression,
            DebugEvaluationResultFields fields,
            CancellationToken cancellationToken = default(CancellationToken)
        ) {
            return EvaluateAsync(null, expression, null, null, fields, null, cancellationToken);
        }

        public Task<DebugEvaluationResult> EvaluateAsync(
            string expression,
            DebugEvaluationResultFields fields,
            int? reprMaxLength,
            CancellationToken cancellationToken = default(CancellationToken)
        ) {
            return EvaluateAsync(null, expression, null, null, fields, reprMaxLength, cancellationToken);
        }

        public async Task<DebugEvaluationResult> EvaluateAsync(
            DebugStackFrame stackFrame,
            string expression,
            string name,
            string env,
            DebugEvaluationResultFields fields,
            int? reprMaxLength = null,
            CancellationToken cancellationToken = default(CancellationToken)
        ) {
            ThrowIfDisposed();

            await TaskUtilities.SwitchToBackgroundThread();
            await InitializeAsync(cancellationToken);

            env = env ?? stackFrame?.EnvironmentExpression ?? "NULL";
            var code = Invariant($"rtvs:::eval_and_describe({expression.ToRStringLiteral()}, {env},, {fields.ToRVector()},, {reprMaxLength})");
            var jEvalResult = await InvokeDebugHelperAsync<JObject>(code, cancellationToken);
            return DebugEvaluationResult.Parse(stackFrame, name, jEvalResult);
        }

        public async Task BreakAsync(CancellationToken ct = default(CancellationToken)) {
            await TaskUtilities.SwitchToBackgroundThread();

            // Evaluation will not end until after Browse> is responded to, but this method must indicate completion
            // as soon as the prompt appears. So don't wait for this, but wait for the prompt instead.
            RSession.EvaluateAsync("browser()", REvaluationKind.Reentrant, ct)
                .SilenceException<MessageTransportException>().DoNotWait();

            // Wait until prompt appears, but don't actually respond to it.
            using (var inter = await RSession.BeginInteractionAsync(true, ct)) { }
        }

        public async Task ContinueAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            await TaskUtilities.SwitchToBackgroundThread();
            ExecuteBrowserCommandAsync("c", null, cancellationToken)
                .SilenceException<MessageTransportException>()
                .SilenceException<RException>()
                .DoNotWait();
        }

        public Task<bool> StepIntoAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            return StepAsync(cancellationToken, "s");
        }

        public Task<bool> StepOverAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            return StepAsync(cancellationToken, "n");
        }

        public Task<bool> StepOutAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            return StepAsync(cancellationToken, "c", async inter => {
                using (var eval = await RSession.BeginEvaluationAsync(cancellationToken)) {
                    // EvaluateAsync will push a new toplevel context on the context stack before
                    // evaluating the expression, so tell browser_set_debug to skip 1 toplevel context
                    // before locating the target context for step-out.
                    var res = await eval.EvaluateAsync("rtvs:::browser_set_debug(1, 1)", REvaluationKind.Normal);
                    Trace.Assert(res.ParseStatus == RParseStatus.OK);

                    if (res.ParseStatus != RParseStatus.OK || res.Error != null) {
                        _stepTcs.TrySetResult(false);
                        return false;
                    }

                    return true;
                }
            });
        }

        /// <returns>
        /// <c>true</c> if step completed successfully, and <c>false</c> if it was interrupted midway by something
        /// else pausing the process, such as a breakpoint.
        /// </returns>
        private async Task<bool> StepAsync(CancellationToken cancellationToken, string command, Func<IRSessionInteraction, Task<bool>> prepare = null) {
            ThrowIfDisposed();
            await TaskUtilities.SwitchToBackgroundThread();

            _stepTcs = new TaskCompletionSource<bool>();
            ExecuteBrowserCommandAsync(command, prepare, cancellationToken)
                .SilenceException<MessageTransportException>()
                .SilenceException<RException>()
                .DoNotWait();
            return await _stepTcs.Task;
        }

        public bool CancelStep() {
            ThrowIfDisposed();

            if (_stepTcs == null) {
                return false;
            }

            _stepTcs.TrySetCanceled();
            _stepTcs = null;
            return true;
        }

        /// <summary>
        /// Retrieve the current call stack, in call order (i.e. the current active frame is last, the one that called it is second to last etc).
        /// </summary>
        /// <param name="skipSourceFrames">
        /// If <see langword="true"/>, excludes frames that belong to <c>source()</c> or <c>rtvs:::debug_source()</c> internal machinery at the bottom of the stack;
        /// the first reported frame will be the one with sourced code.
        /// </param>
        /// <returns></returns>
        public async Task<IReadOnlyList<DebugStackFrame>> GetStackFramesAsync(bool skipSourceFrames = true, CancellationToken cancellationToken = default(CancellationToken)) {
            ThrowIfDisposed();

            await TaskUtilities.SwitchToBackgroundThread();
            await InitializeAsync(cancellationToken);

            var jFrames = await InvokeDebugHelperAsync<JArray>("rtvs:::describe_traceback()", cancellationToken);
            Trace.Assert(jFrames.All(t => t is JObject), "rtvs:::describe_traceback(): array of objects expected.\n\n" + jFrames);

            var stackFrames = new List<DebugStackFrame>();

            DebugStackFrame lastFrame = null;
            int i = 0;
            foreach (JObject jFrame in jFrames) {
                var fallbackFrame = (_bpHitFrame != null && _bpHitFrame.Index == i) ? _bpHitFrame : null;
                lastFrame = new DebugStackFrame(this, i, lastFrame, jFrame, fallbackFrame);
                stackFrames.Add(lastFrame);
                ++i;
            }

            if (skipSourceFrames) {
                var firstFrame = stackFrames.FirstOrDefault();
                if (firstFrame != null && firstFrame.IsGlobal && firstFrame.Call != null) {
                    if (firstFrame.Call.StartsWith("source(") || firstFrame.Call.StartsWith("rtvs::debug_source(")) {
                        // Skip everything until the first frame that has a line number - that will be the sourced code.
                        stackFrames = stackFrames.SkipWhile(f => f.LineNumber == null).ToList();
                    }
                }
            }

            return stackFrames;
        }

        public async Task EnableBreakpointsAsync(bool enable, CancellationToken ct = default(CancellationToken)) {
            ThrowIfDisposed();
            await TaskUtilities.SwitchToBackgroundThread();
            using (var eval = await RSession.BeginEvaluationAsync(ct)) {
                await eval.EvaluateAsync($"rtvs:::enable_breakpoints({(enable ? "TRUE" : "FALSE")})", REvaluationKind.Mutating);
            }
        }

        public async Task<DebugBreakpoint> CreateBreakpointAsync(DebugBreakpointLocation location, CancellationToken cancellationToken = default(CancellationToken)) {
            ThrowIfDisposed();

            await TaskUtilities.SwitchToBackgroundThread();
            await InitializeAsync(cancellationToken);

            DebugBreakpoint bp;
            if (!_breakpoints.TryGetValue(location, out bp)) {
                bp = new DebugBreakpoint(this, location);
                _breakpoints.Add(location, bp);
            }

            await bp.SetBreakpointAsync(cancellationToken);
            return bp;
        }

        internal void RemoveBreakpoint(DebugBreakpoint breakpoint) {
            Trace.Assert(breakpoint.Session == this);
            _breakpoints.Remove(breakpoint.Location);
        }

        private void InterruptBreakpointHitProcessing() {
            _bpHitFrame = null;
        }

        private void ProcessBrowsePrompt(IReadOnlyList<IRContext> contexts) {
            if (!contexts.IsBrowser()) {
                InterruptBreakpointHitProcessing();
                return;
            }

            RSession.BeginInteractionAsync().ContinueWith(async t => {
                using (var inter = await t) {
                    if (inter.Contexts != contexts) {
                        // Someone else has already responded to this interaction.
                        InterruptBreakpointHitProcessing();
                        return;
                    } else {
                        await ProcessBrowsePromptWorker(inter);
                    }
                }
            }).DoNotWait();
        }

        private async Task ProcessBrowsePromptWorker(IRSessionInteraction inter) {
            var frames = await GetStackFramesAsync();

            // If there's .doTrace(rtvs:::breakpoint) anywhere on the stack, we're inside the internal machinery
            // that triggered Browse> prompt when hitting a breakpoint. We need to step out of it until we're
            // back at the frame where the breakpoint was actually set, so that those internal frames do not show
            // on the call stack, and further stepping does not try to step through them. 
            // Since browserSetDebug-based step out is not reliable in the presence of loops, we'll just keep
            // stepping over with "n" until we're all the way out. Every step will trigger a new prompt, and
            // we will come back to this method again.
            var doTraceFrame = frames.FirstOrDefault(frame => frame.FrameKind == DebugStackFrameKind.DoTrace);
            if (doTraceFrame != null) {
                // Save the .doTrace frame so that we can report file / line number info correctly later, once we're fully stepped out.
                // TODO: remove this hack when injected breakpoints get proper source info (#570).
                _bpHitFrame = doTraceFrame;

                await inter.RespondAsync(Invariant($"n\n"));
                return;
            }

            IReadOnlyCollection<DebugBreakpoint> breakpointsHit = null;
            var lastFrame = frames.LastOrDefault();
            if (lastFrame != null) {
                // Report breakpoints first, so that by the time step completion is reported, all actions associated
                // with breakpoints (e.g. printing messages for tracepoints) have already been completed.
                if (lastFrame.FileName != null && lastFrame.LineNumber != null) {
                    var location = new DebugBreakpointLocation(lastFrame.FileName, lastFrame.LineNumber.Value);
                    DebugBreakpoint bp;
                    if (_breakpoints.TryGetValue(location, out bp)) {
                        bp.RaiseBreakpointHit();
                        breakpointsHit = Enumerable.Repeat(bp, bp.UseCount).ToArray();
                    }
                }
            }

            bool isStepCompleted = false;
            if (_stepTcs != null) {
                var stepTcs = _stepTcs;
                _stepTcs = null;
                stepTcs.TrySetResult(breakpointsHit == null || breakpointsHit.Count == 0);
                isStepCompleted = true;
            }

            EventHandler<DebugBrowseEventArgs> browse;
            lock (_browseLock) {
                browse = _browse;
            }

            var eventArgs = new DebugBrowseEventArgs(inter.Contexts, isStepCompleted, breakpointsHit);
            _currentBrowseEventArgs = eventArgs;
            browse?.Invoke(this, eventArgs);
        }

        private void RSession_Connected(object sender, EventArgs e) {
            lock (_initializeLock) {
                _initializeTask = null;
            }

            InitializeAsync().DoNotWait();
        }

        private void RSession_BeforeRequest(object sender, RRequestEventArgs e) {
            _initialPromptCts.Cancel();
            ProcessBrowsePrompt(e.Contexts);
        }

        private void RSession_AfterRequest(object sender, RRequestEventArgs e) {
            _currentBrowseEventArgs = null;
        }
    }

    public class REvaluationException : Exception {
        public REvaluationResult Result { get; }

        public REvaluationException(REvaluationResult result) {
            Result = result;
        }
    }

    public class DebugBrowseEventArgs : EventArgs {
        public IReadOnlyList<IRContext> Contexts { get; }
        public bool IsStepCompleted { get; }
        public IReadOnlyCollection<DebugBreakpoint> BreakpointsHit { get; }

        public DebugBrowseEventArgs(IReadOnlyList<IRContext> contexts, bool isStepCompleted, IReadOnlyCollection<DebugBreakpoint> breakpointsHit) {
            Contexts = contexts;
            IsStepCompleted = isStepCompleted;
            BreakpointsHit = breakpointsHit ?? new DebugBreakpoint[0];
        }
    }
}
