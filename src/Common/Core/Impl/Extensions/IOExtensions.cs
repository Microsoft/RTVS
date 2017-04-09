﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Common.Core.IO;

namespace Microsoft.Common.Core {
    public static class IOExtensions {
        public static string MakeRelativePath(this string path, string basePath) {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath)) {
                return path;
            }
            var bp = basePath.EndsWith(Path.DirectorySeparatorChar) ? basePath : basePath + Path.DirectorySeparatorChar;
            if (path.PathEquals(bp)) {
                return string.Empty;
            }
            return path.StartsWithIgnoreCase(bp) ? path.Substring(bp.Length) : path;
        }

        public static bool ExistsOnPath(string fileName) {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName) {
            if (File.Exists(fileName)) {
                return Path.GetFullPath(fileName);
            }

            var values = Environment.GetEnvironmentVariable("PATH");
            var paths = values.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths) {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            }
            return null;
        }

        /// <summary>
        /// Recursively enumerate sub-directories and gets all files for the given <paramref name="basedir"/>
        /// </summary>
        public static IEnumerable<IFileSystemInfo> GetAllFiles(this IDirectoryInfo basedir) {
            List<IFileSystemInfo> files = new List<IFileSystemInfo>();
            Queue<IDirectoryInfo> dirs = new Queue<IDirectoryInfo>();
            dirs.Enqueue(basedir);
            while (dirs.Count > 0) {
                var dir = dirs.Dequeue();
                foreach (var info in dir.EnumerateFileSystemInfos()) {
                    var subdir = info as IDirectoryInfo;
                    if (subdir != null) {
                        dirs.Enqueue(subdir);
                    } else {
                        files.Add(info);
                    }
                }
            }
            return files;
        }

        public static string TrimTrailingSlash(this string path) {
            if (!string.IsNullOrEmpty(path) && (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))) {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            return path;
        }

        public static bool PathEquals(this string path, string otherPath) {
            var pathLength = GetTrailingSlashTrimmedPathLength(path);
            var otherPathLength = GetTrailingSlashTrimmedPathLength(otherPath);

            return pathLength == otherPathLength && (pathLength == 0 || string.Compare(path, 0, otherPath, 0, otherPathLength, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private static int GetTrailingSlashTrimmedPathLength(string path) {
            if (string.IsNullOrEmpty(path)) {
                return 0;
            }
            if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)) {
                return path.Length - 1;
            }
            return path.Length;
        }

        public static string EnsureTrailingSlash(this string path) {
            if (!string.IsNullOrEmpty(path)) {
                if (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.AltDirectorySeparatorChar) {
                    return path;
                }
                char slash = path.IndexOf(Path.AltDirectorySeparatorChar) >= 0 ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar;
                return path + slash;
            }
            return Path.DirectorySeparatorChar.ToString();
        }

        public static bool HasReadPermissions(this string path) {
            try {
                if (Directory.Exists(path)) {
                    Directory.GetFiles(path);
                    return true;
                }
            } catch(IOException) { } catch(UnauthorizedAccessException) { }
            return false;
        }

        public static bool HasWritePermission(this string path) {
            var accessControlList = new DirectorySecurity(path, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
            var accessRules = accessControlList.GetAccessRules(true, true, typeof(SecurityIdentifier));

            bool allowWrite = false;
            bool denyWrite = false;
            var currentUser = WindowsIdentity.GetCurrent();
            var currentPrincipal = new WindowsPrincipal(currentUser);

            foreach (FileSystemAccessRule rule in accessRules) {
                if ((FileSystemRights.Write & rule.FileSystemRights) == FileSystemRights.Write) {
                    if (currentUser.User.Equals(rule.IdentityReference) ||
                        currentPrincipal.IsInRole((SecurityIdentifier)rule.IdentityReference)) {
                        allowWrite = rule.AccessControlType == AccessControlType.Allow;
                        denyWrite = rule.AccessControlType == AccessControlType.Deny;
                    }
                }
            }
            return allowWrite && !denyWrite;
        }
    }
}
