﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Logging;
using Microsoft.VisualStudio.R.Packages.R;

namespace Microsoft.VisualStudio.R.Package.Feedback {
    internal sealed class SendFrownCommand : SendMailCommand {
        //TODO: localize
        private const string _disclaimer =
"Please briefly describe what you were doing that led to the issue if applicable.\r\n\r\n" +
"Note that the data contained in the attached logs includes " +
"your command history as well as all output displayed in the R Interactive Window.";

        public SendFrownCommand() :
            base(RGuidList.RCmdSetGuid, RPackageCommandId.icmdSendFrown) {
        }

        internal override void SetStatus() {
            Enabled = true;
        }

        internal override void Handle() {
            string zipPath = DiagnosticLogs.Collect();
            SendMail(_disclaimer, "RTVS Frown", zipPath);
        }
    }
}
