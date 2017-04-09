﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Runtime.CompilerServices;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Enums;
using Microsoft.Common.Core.Extensions;
using Microsoft.Common.Core.Logging;
using Microsoft.Common.Core.Shell;
using Microsoft.R.Components.Settings;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.R.Package.Options.Attributes;
using Microsoft.VisualStudio.R.Package.Options.R.Tools;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Package.Telemetry;
using Microsoft.VisualStudio.Shell;
using static System.FormattableString;

namespace Microsoft.VisualStudio.R.Package.Options.R {
    public class RToolsOptionsPage : DialogPage {
        private readonly IRSettings _settings;
        private readonly SettingsHolder _holder;

        public RToolsOptionsPage() {
            _settings = VsAppShell.Current.GetService<IRSettings>();
            _holder = new SettingsHolder(_settings);
        }

        [LocCategory(nameof(Resources.Settings_WorkspaceCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_ShowWorkspaceSwitchConfirmationDialog))]
        [LocDescription(nameof(Resources.Settings_ShowWorkspaceSwitchConfirmationDialog_Description))]
        [TypeConverter(typeof(YesNoTypeConverter))]
        [DefaultValue(YesNoAsk.Yes)]
        public YesNo ShowWorkspaceSwitchConfirmationDialog {
            get { return _holder.GetValue(YesNo.Yes); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_WorkspaceCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_ShowSaveOnResetConfirmationDialog))]
        [LocDescription(nameof(Resources.Settings_ShowSaveOnResetConfirmationDialog_Description))]
        [TypeConverter(typeof(YesNoTypeConverter))]
        [DefaultValue(YesNoAsk.Yes)]
        public YesNo ShowSaveOnResetConfirmationDialog {
            get { return _holder.GetValue(YesNo.Yes); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_WorkspaceCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_LoadRDataOnProjectLoad))]
        [LocDescription(nameof(Resources.Settings_LoadRDataOnProjectLoad_Description))]
        [TypeConverter(typeof(YesNoAskTypeConverter))]
        [DefaultValue(YesNoAsk.No)]
        public YesNoAsk LoadRDataOnProjectLoad {
            get { return _holder.GetValue(YesNoAsk.No); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_WorkspaceCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_SaveRDataOnProjectUnload))]
        [LocDescription(nameof(Resources.Settings_SaveRDataOnProjectUnload_Description))]
        [TypeConverter(typeof(YesNoAskTypeConverter))]
        [DefaultValue(YesNoAsk.No)]
        public YesNoAsk SaveRDataOnProjectUnload {
            get { return _holder.GetValue(YesNoAsk.No); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HistoryCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_AlwaysSaveHistory))]
        [LocDescription(nameof(Resources.Settings_AlwaysSaveHistory_Description))]
        [DefaultValue(true)]
        public bool AlwaysSaveHistory {
            get { return _holder.GetValue(true); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HistoryCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_ClearFilterOnAddHistory))]
        [LocDescription(nameof(Resources.Settings_ClearFilterOnAddHistory_Description))]
        [DefaultValue(true)]
        public bool ClearFilterOnAddHistory {
            get { return _holder.GetValue(true); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HistoryCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_MultilineHistorySelection))]
        [LocDescription(nameof(Resources.Settings_MultilineHistorySelection_Description))]
        [DefaultValue(true)]
        public bool MultilineHistorySelection {
            get { return _holder.GetValue(true); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_REngineCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_CranMirror))]
        [LocDescription(nameof(Resources.Settings_CranMirror_Description))]
        [TypeConverter(typeof(CranMirrorTypeConverter))]
        [DefaultValue(null)]
        public string CranMirror {
            get { return _holder.GetValue<string>(); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_REngineCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_WorkingDirectory))]
        [LocDescription(nameof(Resources.Settings_WorkingDirectory_Description))]
        [Editor(typeof(BrowserForFolderUIEditor), typeof(UITypeEditor))]
        [DefaultValue("~")]
        public string WorkingDirectory {
            get { return _holder.GetValue("~", "WorkingDirectory"); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_REngineCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_RCodePage))]
        [LocDescription(nameof(Resources.Settings_RCodePage_Description))]
        [TypeConverter(typeof(EncodingTypeConverter))]
        [DefaultValue(0)]
        public int RCodePage {
            get { return _holder.GetValue(0); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_DebuggingCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_EvaluateActiveBindings))]
        [LocDescription(nameof(Resources.Settings_EvaluateActiveBindings_Description))]
        [DefaultValue(true)]
        public bool EvaluateActiveBindings {
            get { return _holder.GetValue(true); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_DebuggingCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_ShowDotPrefixedVariables))]
        [LocDescription(nameof(Resources.Settings_ShowDotPrefixedVariables_Description))]
        [DefaultValue(false)]
        public bool ShowDotPrefixedVariables {
            get { return _holder.GetValue(false); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HelpCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_HelpBrowser))]
        [LocDescription(nameof(Resources.Settings_HelpBrowser_Description))]
        [TypeConverter(typeof(HelpBrowserTypeConverter))]
        [DefaultValue(HelpBrowserType.Automatic)]
        public HelpBrowserType HelpBrowserType {
            get { return _holder.GetValue(HelpBrowserType.Automatic); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HelpCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_WebHelpSearchString))]
        [LocDescription(nameof(Resources.Settings_WebHelpSearchString_Description))]
        [DefaultValue("R site:stackoverflow.com")]
        public string WebHelpSearchString {
            get { return _holder.GetValue("R site:stackoverflow.com", "WebHelpSearchString"); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HelpCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_WebHelpSearchBrowserType))]
        [LocDescription(nameof(Resources.Settings_WebHelpSearchBrowserType_Description))]
        [TypeConverter(typeof(BrowserTypeConverter))]
        [DefaultValue(BrowserType.Internal)]
        public BrowserType WebHelpSearchBrowserType {
            get { return _holder.GetValue(BrowserType.Internal); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_HtmlCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_HtmlBrowserType))]
        [LocDescription(nameof(Resources.Settings_HtmlBrowserType_Description))]
        [TypeConverter(typeof(BrowserTypeConverter))]
        [DefaultValue(BrowserType.External)]
        public BrowserType HtmlBrowserType {
            get { return _holder.GetValue(BrowserType.External); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_MarkdownCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_MarkdownBrowserType))]
        [LocDescription(nameof(Resources.Settings_MarkdownBrowserType_Description))]
        [TypeConverter(typeof(BrowserTypeConverter))]
        [DefaultValue(BrowserType.External)]
        public BrowserType MarkdownBrowserType {
            get { return _holder.GetValue(BrowserType.External); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_GeneralCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_SurveyNewsCheck))]
        [LocDescription(nameof(Resources.Settings_SurveyNewsCheck_Description))]
        [TypeConverter(typeof(SurveyNewsPolicyTypeConverter))]
        [DefaultValue(SurveyNewsPolicy.CheckOnceWeek)]
        public SurveyNewsPolicy SurveyNewsCheck {
            get { return _holder.GetValue(SurveyNewsPolicy.CheckOnceWeek); }
            set { _holder.SetValue(value); }
        }

        [LocCategory(nameof(Resources.Settings_LogCategory))]
        [CustomLocDisplayName(nameof(Resources.Settings_LogLevel))]
        [LocDescription(nameof(Resources.Settings_LogLevel_Description))]
        [TypeConverter(typeof(LogVerbosityTypeConverter))]
        [DefaultValue(LogVerbosity.Normal)]
        public LogVerbosity LogVerbosity {
            get { return _holder.GetValue(LogVerbosity.Normal); }
            set { _holder.SetValue(value); }
        }

        /// Overrides default methods since we provide custom settings storage
        public override void LoadSettingsFromStorage() { }
        public override void SaveSettingsToStorage() { }

        protected override void OnApply(PageApplyEventArgs e) {
            if (e.ApplyBehavior == ApplyKind.Apply) {
                _holder.Apply();
                RtvsTelemetry.Current.ReportSettings();
            }
            base.OnApply(e);
        }

        /// <summary>
        /// Holds settings (name/values) while they are being edited. We don't 
        /// want to apply changes to the actual settings until user clicks OK.
        /// </summary>
        class SettingsHolder {
            private readonly IRSettings _settings;
            private readonly IDictionary<string, object> _dict;

            public SettingsHolder(IRSettings settings) {
                _settings = settings;
                _dict = settings.GetPropertyValueDictionary();
            }

            public T GetValue<T>(T defaultValue, [CallerMemberName] string name = null) {
                object value;
                return _dict.TryGetValue(name, out value) ? (T)value : default(T);
            }

            public T GetValue<T>([CallerMemberName] string name = null) => GetValue<T>(default(T), name);

            public void SetValue(object value, [CallerMemberName] string name = null) {
                Debug.Assert(_dict.ContainsKey(name), Invariant($"Unknown setting {name}. RToolsOptionsPage property name does not match IRToolsSettings"));
                _dict[name] = value;
            }

            public void Apply() {
                _settings.SetProperties(_dict);
                _settings.SaveSettingsAsync().DoNotWait();
            }
        }
    }
}
