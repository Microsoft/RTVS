﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Common.Core.Test.Script;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Core.AST;
using Microsoft.R.Core.Parser;
using Microsoft.R.Editor.QuickInfo;
using Microsoft.R.Editor.Signatures;
using Microsoft.R.Editor.Test.Utility;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Test.Script;
using Microsoft.UnitTests.Core.Mef;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.Editor.Mocks;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.R.Editor.Test.QuickInfo {
    [ExcludeFromCodeCoverage]
    [Category.R.Signatures]
    [Collection(CollectionNames.NonParallel)]
    public class FunctionIndexTest : FunctionIndexBasedTest {
        class Session {
            public AstRoot Ast;
            public ITrackingSpan ApplicableSpan;
            public List<object> QuickInfoContent;
            public ITextBuffer TextBuffer;
        }

        public FunctionIndexTest(IExportProvider exportProvider) : base(exportProvider) { }

        [CompositeTest]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CacheTest(bool cached) {
            if (!cached) {
                Support.Help.Packages.PackageIndex.ClearCache();
            }

            string content = @"x <- as.matrix(x)";
            var session = await TriggerSessionAsync(content, 15);
            var parametersInfo = SignatureHelp.GetParametersInfoFromBuffer(session.Ast, session.TextBuffer.CurrentSnapshot, 10);

            session.ApplicableSpan.Should().NotBeNull();
            session.QuickInfoContent.Should().ContainSingle()
                .Which.ToString().Should().StartWith("as.matrix(x, ...)");
        }

        [Test]
        public async Task AliasTest() {
            // 'as.Date.character' RD contains no function info for 'as.Date.character', but the one for 'as.Date'
            // and as.Date.character appears as alias. We verify as.Date.character is shown in the signature info.
            string content = @"x <- as.Date.character(x)";

            var session = await TriggerSessionAsync(content, 23);
            var parametersInfo = SignatureHelp.GetParametersInfoFromBuffer(session.Ast, session.TextBuffer.CurrentSnapshot, 10);

            session.ApplicableSpan.Should().NotBeNull();
            session.QuickInfoContent.Should().ContainSingle()
                .Which.ToString().Should().StartWith("as.Date.character(x, ...)");
        }

        [CompositeTest]
        [InlineData("sin(cos(x), y)", 2, "sin(")]
        [InlineData("sin(cos(x), y)", 5, "cos(")]
        [InlineData("sin(cos(x), y)", 8, "cos(")]
        [InlineData("sin(cos(x), y)", 11, "sin(")]
        [InlineData("sin(cos(tan(x)), y)", 9, "tan(")]
        [InlineData("cos(de, tan(x, hi(y)), y)", 5, "cos(")]
        [InlineData("cos(de, tan(x, sin(y)), y)", 9, "tan(")]
        [InlineData("cos(de, tan(x, sin(y)), y)", 15, "sin(")]
        [InlineData("cos(de, tan(x, sin(y), yy), zz)", 22, "tan(")]
        [InlineData("cos(de, tan(x, sin(y), yy), zz)", 27, "cos(")]
        public async Task NestedCallTest(string content, int position, string name) {
            var session = await TriggerSessionAsync(content, position);
            session.QuickInfoContent.Should().ContainSingle().Which.ToString().Should().StartWith(name);
        }

        [Test]
        public async Task NonUniqueNameTest() {
            string content = @"x <- select()";

            using (new RHostScript(Workflow.RSessions)) {
                //await Workflow.RSession.ExecuteAsync("install.packages('dplyr')");

                var session = await TriggerSessionAsync(content, 12);
                SignatureHelp.GetParametersInfoFromBuffer(session.Ast, session.TextBuffer.CurrentSnapshot, 10);

                session.ApplicableSpan.Should().NotBeNull();
                session.QuickInfoContent.Should().BeEmpty();

                await Workflow.RSession.ExecuteAsync("library(MASS)");
                EventsPump.DoEvents(500);
                session = await TriggerSessionAsync(content, 12);

                session.ApplicableSpan.Should().NotBeNull();
                session.QuickInfoContent.Should().ContainSingle().Which.ToString().Should().StartWith("select(formula");

                await Workflow.RSession.ExecuteAsync("library(dplyr)");
                EventsPump.DoEvents(500);
                session = await TriggerSessionAsync(content, 12);

                session.ApplicableSpan.Should().NotBeNull();
                session.QuickInfoContent.Should().ContainSingle().Which.ToString().Should().StartWith("select(.data");
            }
        }

        [Test]
        public async Task LoadUnloadPackageTest() {
            string content = @"do()";

            using (var hostScript = new RHostScript(Workflow.RSessions)) {
                //await Workflow.RSession.ExecuteAsync("install.packages('dplyr')");

                var session = await TriggerSessionAsync(content, 3);
                SignatureHelp.GetParametersInfoFromBuffer(session.Ast, session.TextBuffer.CurrentSnapshot, 10);

                session.ApplicableSpan.Should().NotBeNull();
                session.QuickInfoContent.Should().BeEmpty();

                await Workflow.RSession.ExecuteAsync("library(dplyr)");
                session = await TriggerSessionAsync(content, 3);

                session.ApplicableSpan.Should().NotBeNull();
                session.QuickInfoContent.Should().ContainSingle().Which.ToString().Should().StartWith("do(.data");

                await Workflow.RSession.ExecuteAsync("detach(\"package:dplyr\", unload = TRUE)");
                EventsPump.DoEvents(1000);

                session = await TriggerSessionAsync(content, 3);
                session.QuickInfoContent.Should().BeEmpty();
            }
        }

        private async Task<Session> TriggerSessionAsync(string content, int caretPosition) {
            var s = new Session {
                Ast = RParser.Parse(content),
                TextBuffer = new TextBufferMock(content, RContentTypeDefinition.ContentType)
            };
            QuickInfoSource quickInfoSource = new QuickInfoSource(s.TextBuffer, EditorShell);
            QuickInfoSessionMock quickInfoSession = new QuickInfoSessionMock(s.TextBuffer, caretPosition);
            s.QuickInfoContent = new List<object>();

            quickInfoSession.TriggerPoint = new SnapshotPoint(s.TextBuffer.CurrentSnapshot, caretPosition);
            s.ApplicableSpan = await quickInfoSource.AugmentQuickInfoSessionAsync(s.Ast, s.TextBuffer, caretPosition, quickInfoSession, s.QuickInfoContent);

            return s;
        }
    }
}
