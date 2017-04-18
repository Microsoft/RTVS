﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core.Services;
using Microsoft.Markdown.Editor.Commands;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.VisualStudio.R.Package.Browsers;
using Microsoft.VisualStudio.R.Package.Publishing.Definitions;
using Microsoft.VisualStudio.Text.Editor;
using static System.FormattableString;

namespace Microsoft.VisualStudio.R.Package.Publishing.Commands {
    internal sealed class PreviewHtmlCommand : PreviewCommand {
        private readonly IWebBrowserServices _wbs;

        public PreviewHtmlCommand(
            ITextView textView,
            IRInteractiveWorkflowVisualProvider workflowProvider, IServiceContainer services)
            : base(textView, (int)MdPackageCommandId.icmdPreviewHtml, workflowProvider, services) {
            _wbs = services.GetService<IWebBrowserServices>();
        }

        protected override string FileExtension=> "html";
        protected override PublishFormat Format=> PublishFormat.Html;

        protected override void LaunchViewer(string fileName) {
            _wbs.OpenBrowser(WebBrowserRole.Markdown, Invariant($"file://{fileName}"));
        }
    }
}
