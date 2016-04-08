﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Common.Core;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.R.Package.DataInspect.DataSource;
using Microsoft.VisualStudio.R.Package.Shell;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace Microsoft.VisualStudio.R.Package.DataInspect.DataImport {
    /// <summary>
    /// Interaction logic for ImportDataWindow.xaml
    /// </summary>
    public partial class ImportDataWindow : DialogWindow {
        public IDictionary<string, string> Separators { get; } = new Dictionary<string, string> {
            ["White space"] = "",
            ["Comma (,)"] = ",",
            ["Semicolon (;)"] = ";",
            [@"Tab (\t)"] = "\t"
        };

        public IDictionary<string, string> Decimals { get; } = new Dictionary<string, string> {
            ["Period (.)"] = ".",
            ["Comma (,)"] = ","
        };

        public IDictionary<string, string> Quotes { get; } = new Dictionary<string, string> {
            ["Double quote (\")"] = "\"",
            ["Single quote (')"] = "'",
            ["None"] = ""
        };

        public IDictionary<string, string> Comments { get; } = new Dictionary<string, string> {
            ["None"] = "",
            ["#"] = "#",
            ["!"] = "!",
            ["%"] = "%",
            ["@"] = "@",
            ["/"] = "/",
            ["~"] = "~"
        };

        public IDictionary<string, string> RowNames { get; } = new Dictionary<string, string> {
            ["Automatic"] = null,
            ["Use first column"] = "1",
            ["Use number"] = "NULL"
        };

        public ImportDataWindow() {
            InitializeComponent();
            SetEncodingComboBoxAsync().DoNotWait();
        }

        public ImportDataWindow(string filePath, string name)
            : this() {
            SetFilePathAsync(filePath, name).DoNotWait();
        }

        private IRSession GetRSession() {
            IRSession rSession = VsAppShell.Current.ExportProvider.GetExportedValue<IRInteractiveWorkflowProvider>().GetOrCreate().RSession;
            if (rSession == null) {
                throw new InvalidOperationException(Invariant($"{nameof(IRSessionProvider)} failed to return RSession for importing data set"));
            }
            return rSession;
        }

        private string BuildCommandLine(bool preview) {
            if (string.IsNullOrEmpty(FilePathBox.Text)) {
                return null;
            }

            var input = new Dictionary<string, string> {
                ["file"] = FilePathBox.Text.ToRStringLiteral(),
                ["header"] = (HeaderCheckBox.IsChecked != null && HeaderCheckBox.IsChecked.Value).ToString().ToUpperInvariant(),
                ["row.names"] = GetSelectedValue(RowNamesComboBox),
                ["encoding"] = (EncodingComboBox.SelectedItem as string)?.ToRStringLiteral(),
                ["sep"] = GetSelectedValue(SeparatorComboBox),
                ["dec"] = GetSelectedValue(DecimalComboBox),
                ["quote"] = GetSelectedValue(QuoteComboBox),
                ["comment.char"] = GetSelectedValue(CommentComboBox),
                ["na.strings"] = string.IsNullOrEmpty(NaStringTextBox.Text) ? null : NaStringTextBox.Text.ToRStringLiteral()
            };

            var inputString = string.Join(", ", input
                .Where(kvp => kvp.Value != null)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));

            return preview 
                ? string.Format(CultureInfo.InvariantCulture, "read.csv({0}, nrows=20)", inputString) 
                : string.Format(CultureInfo.InvariantCulture, "`{0}` <- read.csv({1})", VariableNameBox.Text, inputString);
        }

        private static string GetSelectedValue(ComboBox comboBox) {
            return ((KeyValuePair<string, string>)comboBox.SelectedItem).Value.ToRStringLiteral();
        }

        private void FileOpenButton_Click(object sender, RoutedEventArgs e) {
            string filePath = VsAppShell.Current.BrowseForFileOpen(IntPtr.Zero, "CSV file|*.csv");
            if (!string.IsNullOrEmpty(filePath)) {
                SetFilePathAsync(filePath).DoNotWait();
            }
        }

        private async Task SetFilePathAsync(string filePath, string name = null) {
            FilePathBox.Text = filePath;
            FilePathBox.CaretIndex = FilePathBox.Text.Length;
            FilePathBox.ScrollToEnd();

            VariableNameBox.Text = name ?? Path.GetFileNameWithoutExtension(filePath);
            InputFilePreview.Text = ReadFile(FilePathBox.Text);

            await PreviewAsync();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) {
            RunButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            var expression = BuildCommandLine(false);
            if (expression != null) {
                RunAsync(expression).DoNotWait();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private async Task PreviewAsync() {
            var expression = BuildCommandLine(true);
            if (expression != null) {
                try {
                    var grid = await GridDataSource.GetGridDataAsync(expression, null);

                    PopulateDataFramePreview(grid);
                    ErrorBlock.Visibility = Visibility.Collapsed;
                    DataFramePreview.Visibility = Visibility.Visible;
                } catch (Exception ex) {
                    OnError(ex.Message);
                }
            }
        }

        private async Task RunAsync(string expression) {
            try {
                REvaluationResult result = await EvaluateAsyncAsync(expression);
                if (result.ParseStatus == RParseStatus.OK && result.Error == null) {
                    Close();
                } else {
                    OnError(result.ToString());
                }
            } catch (Exception ex) {
                OnError(ex.Message);
            }
        }

        private async Task SetEncodingComboBoxAsync() {
            try {
                REvaluationResult result = await CallIconvListAsync();

                if (result.ParseStatus == RParseStatus.OK && result.Error == null) {
                    var jarray = result.JsonResult as JArray;
                    if (jarray != null) {
                        PopulateEncoding(jarray);
                    }
                } else {
                    OnError(result.ToString());
                }
            } catch (Exception ex) {
                OnError(ex.Message);
            }
        }

        private void OnError(string errorText) {
            ErrorText.Text = errorText;
            ErrorBlock.Visibility = Visibility.Visible;
            DataFramePreview.Visibility = Visibility.Collapsed;
        }

        private async Task<REvaluationResult> CallIconvListAsync() {
            await TaskUtilities.SwitchToBackgroundThread();

            IRSession rSession = GetRSession();
            REvaluationResult result;
            using (var evaluator = await rSession.BeginEvaluationAsync()) {
                result = await evaluator.EvaluateAsync("as.list(iconvlist())", REvaluationKind.Json);
            }

            return result;
        }

        private async Task<REvaluationResult> EvaluateAsyncAsync(string expression) {
            await TaskUtilities.SwitchToBackgroundThread();

            IRSession rSession = GetRSession();
            REvaluationResult result;
            using (var evaluator = await rSession.BeginEvaluationAsync()) {
                result = await evaluator.EvaluateAsync(expression, REvaluationKind.Mutating);
            }

            return result;
        }

        private void PopulateEncoding(JArray jarray) {
            EncodingComboBox.ItemsSource = jarray
                .Select(item => item.Value<string>())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        private void PopulateDataFramePreview(IGridData<string> gridData) {
            var dg = DataFramePreview;
            dg.Columns.Clear();

            for (int i = 0; i < gridData.ColumnHeader.Range.Count; i++) {
                dg.Columns.Add(new DataGridTextColumn() {
                    Header = gridData.ColumnHeader[gridData.ColumnHeader.Range.Start + i],
                    Binding = new Binding(Invariant($"Values[{i}]")),
                });
            }

            var rows = new List<DataFramePreviewRowItem>();
            for (var r = 0; r < gridData.Grid.Range.Rows.Count; r++) {
                var row = new DataFramePreviewRowItem {
                    RowName = gridData.RowHeader[gridData.RowHeader.Range.Start + r]
                };

                for (int c = 0; c < gridData.Grid.Range.Columns.Count; c++) {
                    row.Values.Add(gridData.Grid[gridData.Grid.Range.Rows.Start + r, gridData.Grid.Range.Columns.Start + c]);
                }

                rows.Add(row);
            }
            dg.ItemsSource = rows;
        }

        private string ReadFile(string filePath, int lineCount = 20) {
            StringBuilder sb = new StringBuilder();
            using(var sr = new StreamReader(filePath)) {
                int readCount = 0;
                while(readCount < lineCount) {
                    string read = sr.ReadLine();
                    if (read == null) {
                        break;
                    }
                    sb.AppendLine(read);
                    readCount++;
                }
            }

            return sb.ToString();
        }

        class DataFramePreviewRowItem {
            public string RowName { get; set; }
            public List<string> Values { get; } = new List<string>();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            PreviewAsync().DoNotWait();
        }

        private void HeaderCheckBox_Changed(object sender, RoutedEventArgs e) {
            PreviewAsync().DoNotWait();
        }
    }
}
