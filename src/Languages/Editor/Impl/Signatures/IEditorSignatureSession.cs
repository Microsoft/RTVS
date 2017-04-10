﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.Languages.Editor.Signatures {
    /// <summary>
    /// Represents active function signature session in the editor.
    /// </summary>
    public interface IEditorSignatureSession {
        IEditorView View { get; }
        bool IsDismissed { get; }
    }
}