﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.Shell;
using Microsoft.Common.Core.UI.Commands;
using Microsoft.R.Components.ContentTypes;
using Microsoft.UnitTests.Core.XUnit;
using Xunit;

namespace Microsoft.R.Editor.Application.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [Collection(CollectionNames.NonParallel)]
    public class FormatTest {
        private readonly ICoreShell _coreShell;
        private readonly EditorHostMethodFixture _editorHost;

        public FormatTest(IServiceContainer services, EditorHostMethodFixture editorHost) {
            _coreShell = services.GetService<ICoreShell>();
            _editorHost = editorHost;
        }

        [CompositeTest]
        [Category.Interactive]
        [InlineData("\nwhile (TRUE) {\n        if(x>1) {\n   }\n}", "\nwhile (TRUE) {\n    if (x > 1) {\n    }\n}")]
        [InlineData("if (1 && # comment\n  2) {x<-1}", "if (1 && # comment\n  2) { x <- 1 }")]
        public async Task R_FormatDocument(string original, string expected) {
            using (var script = await _editorHost.StartScript(_coreShell, original, RContentTypeDefinition.ContentType)) {
                script.Execute(VSConstants.VSStd2KCmdID.FORMATDOCUMENT, 50);
                string actual = script.EditorText;
                actual.Should().Be(expected);
            }
        }
    }
}
