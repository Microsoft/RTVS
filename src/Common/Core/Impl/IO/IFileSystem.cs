﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Common.Core.IO {
    public interface IFileSystem {
        IFileSystemWatcher CreateFileSystemWatcher(string directory, string filter);
        IDirectoryInfo GetDirectoryInfo(string directoryPath);
        bool FileExists(string fullPath);
        bool DirectoryExists(string fullPath);
        FileAttributes GetFileAttributes(string fullPath);

        IEnumerable<string> FileReadAllLines(string path);
        void FileWriteAllLines(string path, IEnumerable<string> contents);

        IFileVersionInfo GetVersionInfo(string path);
    }
}
