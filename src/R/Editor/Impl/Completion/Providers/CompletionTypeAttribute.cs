﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.R.Editor.Completion.Providers {
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CompletionTypeAttribute : Attribute {
        public string CompletionType { get; private set; }

        public CompletionTypeAttribute(string completionType) {
            CompletionType = completionType;
        }
    }
}
