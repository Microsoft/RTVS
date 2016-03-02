﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.VisualStudio.R.Package.DataInspect {
    internal class PointChangedEventArgs : EventArgs {
        public PointChangedEventArgs(ScrollDirection direction) {
            Direction = direction;
        }

        public ScrollDirection Direction { get; }
    }
}
