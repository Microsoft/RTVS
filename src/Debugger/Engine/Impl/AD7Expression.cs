﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.R.Debugger.Engine {
    internal class AD7Expression : IDebugExpression2 {
        private readonly string _expression;
        private CancellationTokenSource _cts;

        public AD7StackFrame StackFrame { get; }

        public AD7Expression(AD7StackFrame stackFrame, string expression) {
            _expression = expression;
            StackFrame = stackFrame;
        }

        int IDebugExpression2.Abort() {
            if (_cts == null) {
                return VSConstants.E_FAIL;
            }

            _cts.Cancel();
            _cts = null;
            return VSConstants.S_OK;
        }

        int IDebugExpression2.EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback) {
            _cts = new CancellationTokenSource();
            Task.Run(async () => {
                try {
                    var res = await StackFrame.StackFrame.EvaluateAsync(_expression, reprMaxLength: AD7Property.ReprMaxLength);
                    _cts.Token.ThrowIfCancellationRequested();
                    var prop = new AD7Property(StackFrame, res);
                    StackFrame.Engine.Send(new AD7ExpressionEvaluationCompleteEvent(this, prop), AD7ExpressionEvaluationCompleteEvent.IID);
                } catch (Exception ex) when (!ex.IsCriticalException()) {
                    StackFrame.Engine.Send(new AD7ExpressionEvaluationCompleteEvent(ex), AD7ExpressionEvaluationCompleteEvent.IID);
                } finally {
                    _cts = null;
                }
            });
            return VSConstants.S_OK;
        }

        int IDebugExpression2.EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult) {
            var res = StackFrame.StackFrame.EvaluateAsync(_expression, reprMaxLength: AD7Property.ReprMaxLength).WaitAndUnwrapExceptions();
            ppResult = new AD7Property(StackFrame, res);
            return VSConstants.S_OK;
        }
    }
}