﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.R.Editor.Application.Test.TestShell;
using Microsoft.R.Editor.ContentType;
using Microsoft.R.Editor.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.R.Editor.Application.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AutoFormatTest {
        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatFunctionBraces() {
            var script = new TestScript(RContentTypeDefinition.ContentType);

            try {
                script.Type("function(a,b){");
                script.DoIdle(300);
                script.Type("{ENTER}a");

                string expected = "function(a, b) {\r\n    a\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces01() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = false;

            try {
                script.Type("if(x>1){ENTER}{");
                script.DoIdle(300);
                script.Type("{ENTER}a");

                string expected = "if (x > 1) {\r\n    a\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces02() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = true;

            try {
                script.Type("if(x>1){ENTER}{");
                script.DoIdle(300);
                script.Type("{ENTER}a");

                string expected = "if (x > 1) \r\n{\r\n    a\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces03() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = false;

            try {
                script.Type("while(true) {");
                script.DoIdle(300);
                script.Type("{ENTER}if(x>1) {");
                script.DoIdle(300);
                script.Type("{ENTER}foo");

                string expected = "while (true) {\r\n    if (x > 1) {\r\n        foo\r\n    }\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces04() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = false;

            try {
                script.Type("while(true) {");
                script.DoIdle(300);
                script.Type("}");

                string expected = "while (true) { }";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces05() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = false;

            try {
                script.Type("while(true) {");
                script.DoIdle(300);
                script.Type("{ENTER}if(x>1) {");
                script.DoIdle(300);
                script.Type("{ENTER}");
                script.Type("}}");

                string expected = "while (true) {\r\n    if (x > 1) {\r\n    }\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces06() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = true;

            try {
                script.Type("x <-function(a) {");
                script.DoIdle(300);
                script.Type("{ENTER}a");

                string expected = "x <- function(a) \r\n{\r\n    a\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces07() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = true;

            try {
                script.Type("x <-function(a,{ENTER}{TAB}b){ENTER}{");
                script.DoIdle(300);
                script.Type("{ENTER}a");

                string expected = "x <- function(a,\r\n    b) \r\n{\r\n    a\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatScopeBraces08() {
            var script = new TestScript("while (true) {\r\n}", RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = true;

            try {
                script.MoveDown();
                script.Enter();

                string expected = "while (true) {\r\n\r\n}";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatIfNoScope() {
            var script = new TestScript(RContentTypeDefinition.ContentType);

            try {
                script.Type("if(x>1)");
                script.DoIdle(300);
                script.Type("{ENTER}a");

                string expected = "if (x > 1)\r\n    a";
                string actual = script.EditorText;

                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }

        [TestMethod]
        [TestCategory("Interactive")]
        public void R_AutoFormatFuncionDefinition01() {
            var script = new TestScript(RContentTypeDefinition.ContentType);
            REditorSettings.FormatOptions.BracesOnNewLine = true;
            string text = "library ( abind){ENTER}x <-function (x,y, wt= NULL, intercept =TRUE, tolerance=1e-07, {ENTER}          yname = NULL){ENTER}{{ENTER}abind(a, )";

            try {
                script.Type(text);
                script.DoIdle(300);

                string actual = script.EditorText;
                string expected =
@"library(abind)
x <- function(x, y, wt = NULL, intercept = TRUE, tolerance = 1e-07,
          yname = NULL) 
{
    abind(a, )
}";
                Assert.AreEqual(expected, actual);
            } finally {
                script.Close();
            }
        }
    }
}
