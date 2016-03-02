﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Common.Core {
    public static class StackExtensions {
        public static IEnumerable<T> PopWhile<T>(this Stack<T> stack, Func<T, bool> predicate) {
            while (stack.Count > 0) {
                var item = stack.Peek();
                if (!predicate(item)) {
                    break;
                }

                yield return stack.Pop();
            }
        }
    }
}
