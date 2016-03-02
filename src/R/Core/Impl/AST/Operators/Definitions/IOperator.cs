﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Core.AST.Definitions;

namespace Microsoft.R.Core.AST.Operators.Definitions {
    public interface IOperator : IRValueNode {
        OperatorType OperatorType { get; }

        IRValueNode LeftOperand { get; set; }

        IRValueNode RightOperand { get; set; }

        int Precedence { get; }

        bool IsUnary { get; }

        Association Association { get; }
    }
}
