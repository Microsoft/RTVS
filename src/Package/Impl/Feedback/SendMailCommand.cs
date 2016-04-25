﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Office.Interop.Outlook;
using Microsoft.R.Actions.Logging;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Interop;
using Microsoft.VisualStudio.R.Package.Shell;

namespace Microsoft.VisualStudio.R.Package.Feedback {
    internal class SendMailCommand : PackageCommand {
        public SendMailCommand(Guid group, int id) :
            base(group, id) { }

        protected static void SendMail(string body, string subject, string attachmentFile) {
            if (attachmentFile != null) {
                IntPtr pidl = IntPtr.Zero;
                try {
                    pidl = NativeMethods.ILCreateFromPath(attachmentFile);
                    if (pidl != IntPtr.Zero) {
                        NativeMethods.SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
                    }
                } finally {
                    if (pidl != IntPtr.Zero) {
                        NativeMethods.ILFree(pidl);
                    }
                }
            }

            Application outlookApp = null;
            try {
                outlookApp = new Application();
            } catch (System.Exception ex) {
                GeneralLog.Write("Unable to start Outlook (exception data follows)");
                GeneralLog.Write(ex);
            }

            if (outlookApp == null) {
                var fallbackWindow = new SendMailFallbackWindow {
                    MessageBody = body
                };

                fallbackWindow.Show();
                fallbackWindow.Activate();

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "mailto:rtvsuserfeedback@microsoft.com?subject={0}&body={1}",
                    Uri.EscapeDataString(subject),
                    Uri.EscapeDataString(body));
                Process.Start(psi);
            } else {
                try {
                    MailItem mail = outlookApp.CreateItem(OlItemType.olMailItem) as MailItem;
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.To = "rtvsuserfeedback@microsoft.com";
                    mail.Display(Modal: false);
                } catch (System.Exception ex) {
                    GeneralLog.Write("Error composing Outlook e-mail (exception data follows)");
                    GeneralLog.Write(ex);
                }
            }
        }
    }
}
