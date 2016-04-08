﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Common.Core;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.R.Package.DataInspect.DataImport {
    /// <summary>
    /// Interaction logic for ImportDataWindow.xaml
    /// </summary>
    public partial class EnterUrl : DialogWindow {
        public EnterUrl() {
            InitializeComponent();
        }

        public string DownloadFilePath { get; private set; }

        public string VariableName { get; private set; }

        public void DeleteTemporaryFile() {
            if (!string.IsNullOrEmpty(DownloadFilePath)) {
                try {
                    File.Delete(DownloadFilePath);
                    DownloadFilePath = null;
                } catch {
                }
            }
        }

        public ICommand EnterKeyCommand { get; } = new DelegateCommand(EnterKeyCommandHandler);

        private static void EnterKeyCommandHandler(object parameter) {
            var me = (EnterUrl)parameter;
            me.OkButton.IsEnabled = false;
            me.CancelButton.IsEnabled = false;
            me.DownloadProgressBar.Value = 0;
            me.DownloadProgressBar.Visibility = Visibility.Visible;
            me.ErrorTextBlock.Visibility = Visibility.Collapsed;
            me.ErrorTextBlock.Text = null;
            me.RunAsync().DoNotWait();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            base.Close();
        }

        private async Task RunAsync() {
            try {
                var temporaryFile = Path.GetTempFileName();
                Uri uri = new Uri(UrlTextBox.Text);
                using (var client = new WebClient()) {
                    client.DownloadProgressChanged += DownloadProgressChanged;

                    await client.DownloadFileTaskAsync(uri, temporaryFile);
                }

                DownloadFilePath = temporaryFile;
                VariableName = Path.GetFileNameWithoutExtension(uri.Segments[uri.Segments.Length - 1]);
                OnSuccess();
            } catch (Exception ex) {
                OnError(ex.Message);
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            DownloadProgressBar.Value = e.ProgressPercentage;
        }

        private void OnSuccess() {
            base.Close();
        }

        private void OnError(string errorText) {
            OkButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            DownloadProgressBar.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = errorText;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void WindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }
    }
}
