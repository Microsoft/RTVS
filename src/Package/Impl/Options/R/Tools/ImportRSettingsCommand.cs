﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Editor.Tasks;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Repl.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.R.Package.Options.R.Tools {
    public sealed class ImportRSettingsCommand : MenuCommand {
        public ImportRSettingsCommand() :
            base(OnCommand, new CommandID(RGuidList.RCmdSetGuid, RPackageCommandId.icmdImportRSettings)) { }

        public static void OnCommand(object sender, EventArgs args) {
            if (MessageButtons.Yes == VsAppShell.Current.ShowMessage(Resources.Warning_SettingsReset, MessageButtons.YesNo)) {
                IVsUIShell shell = VsAppShell.Current.GetGlobalService<IVsUIShell>(typeof(SVsUIShell));
                Guid group = VSConstants.CMDSETID.StandardCommandSet2K_guid;

                string asmDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetAssemblyPath());
                string settingsFilePath1 = Path.Combine(asmDirectory, @"Profiles\", "R.vssettings");
                string settingsFilePath2 = Path.Combine(asmDirectory, @"Profiles\", "RStudioKeyboard.vssettings");
                if (!File.Exists(settingsFilePath1)) {
                    string ideFolder = asmDirectory.Substring(0, asmDirectory.IndexOf(@"\Extensions", StringComparison.OrdinalIgnoreCase));
                    settingsFilePath1 = Path.Combine(ideFolder, @"Profiles\", "R.vssettings");
                }

                object arguments = string.Format(CultureInfo.InvariantCulture, "-import:\"{0}\"", settingsFilePath1);
                shell.PostExecCommand(ref group, (uint)VSConstants.VSStd2KCmdID.ManageUserSettings, 0, ref arguments);

                if (MessageButtons.Yes == VsAppShell.Current.ShowMessage(Resources.Warning_RStudioKeyboardShortcuts, MessageButtons.YesNo)) {
                    arguments = string.Format(CultureInfo.InvariantCulture, "-import:\"{0}\"", settingsFilePath2);
                    shell.PostExecCommand(ref group, (uint)VSConstants.VSStd2KCmdID.ManageUserSettings, 0, ref arguments);
                }

                // Restore Ctrl+Enter if necessary
                IdleTimeAction.Create(() => {
                    ReplShortcutSetting.Close();
                    ReplShortcutSetting.Initialize();
                }, 100, typeof(ImportRSettingsCommand));
            }
        }
    }
}
