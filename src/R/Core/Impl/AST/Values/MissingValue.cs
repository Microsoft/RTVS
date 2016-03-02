﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Core.AST.DataTypes;
using Microsoft.R.Core.AST.Definitions;
using Microsoft.R.Core.Parser;

namespace Microsoft.R.Core.AST.Values {
    /// <summary>
    /// Represents NA value
    /// </summary>
    public sealed class MissingValue : RValueTokenNode<RMissing> {
        public override bool Parse(ParseContext context, IAstNode parent) {
            NodeValue = new RMissing();
            return base.Parse(context, parent);
        }
    }
}
