﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Controller;
using Microsoft.Languages.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Markdown.Editor.Commands {
    /// <summary>
    /// Main R editor command controller
    /// </summary>
    public class MdMainController : ViewController {
        public MdMainController(ITextView textView, ITextBuffer textBuffer)
            : base(textView, textBuffer) {
            ServiceManager.AddService<MdMainController>(this, textView);
        }

        public static MdMainController Attach(ITextView textView, ITextBuffer textBuffer) {
            MdMainController controller = FromTextView(textView);
            if (controller == null) {
                controller = new MdMainController(textView, textBuffer);
            }

            return controller;
        }

        public static MdMainController FromTextView(ITextView textView) {
            return ServiceManager.GetService<MdMainController>(textView);
        }

        public override CommandStatus Status(Guid group, int id) {
            return base.Status(group, id);
        }

        /// <summary>
        /// Disposes main controller and removes it from service manager.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (TextView != null) {
                ServiceManager.RemoveService<MdMainController>(TextView);
            }

            base.Dispose(disposing);
        }
    }
}