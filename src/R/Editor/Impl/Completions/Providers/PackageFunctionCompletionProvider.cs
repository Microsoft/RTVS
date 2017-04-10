﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Common.Core.Imaging;
using Microsoft.Common.Core.Services;
using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Editor.Completions;
using Microsoft.R.Core.AST;
using Microsoft.R.Core.Tokens;
using Microsoft.R.Editor.Functions;
using Microsoft.R.Editor.Snippets;

namespace Microsoft.R.Editor.Completions.Providers {
    /// <summary>
    /// Provides list of functions from installed packages
    /// </summary>
    public class PackageFunctionCompletionProvider : IRCompletionListProvider, IRHelpSearchTermProvider {
        private const int _asyncWaitTimeout = 1000;
        private readonly IIntellisenseRSession _session;
        private readonly ISnippetInformationSourceProvider _snippetInformationSource;
        private readonly IPackageIndex _packageIndex;
        private readonly IFunctionIndex _functionIndex;

        private readonly object _functionGlyph;
        private readonly object _constantGlyph;

        public PackageFunctionCompletionProvider(IServiceContainer serviceContainer) {
            _session = serviceContainer.GetService<IIntellisenseRSession>();
            _snippetInformationSource = serviceContainer.GetService<ISnippetInformationSourceProvider>();
            _packageIndex = serviceContainer.GetService<IPackageIndex>();
            _functionIndex = serviceContainer.GetService<IFunctionIndex>();

            var imageService = serviceContainer.GetService<IImageService>();
            _functionGlyph = imageService.GetImage(ImageType.Method);
            _constantGlyph = imageService.GetImage(ImageType.Constant);
        }

        #region IRCompletionListProvider
        public bool AllowSorting { get; } = true;

        public IReadOnlyCollection<ICompletionEntry> GetEntries(IRCompletionContext context) {
            var completions = new List<ICompletionEntry>();
            var infoSource = _snippetInformationSource?.InformationSource;

            // TODO: this is different in the console window where 
            // packages may have been loaded from the command line. 
            // We need an extensibility point here.
            IEnumerable<IPackageInfo> packages = GetPackages(context);

            // Get list of functions in the package
            foreach (IPackageInfo pkg in packages) {
                Debug.Assert(pkg != null);

                IEnumerable<INamedItemInfo> functions = pkg.Functions;
                if (functions != null) {
                    foreach (INamedItemInfo function in functions) {
                        bool isSnippet = false;
                        // Snippets are suppressed if user typed namespace
                        if (!context.IsCaretInNameSpace() && infoSource != null) {
                            isSnippet = infoSource.IsSnippet(function.Name);
                        }
                        if (!isSnippet) {
                            var glyph = function.ItemType == NamedItemType.Constant ? _constantGlyph : _functionGlyph;
                            var completion = new RFunctionCompletionEntry(function.Name, function.Name.WrapInBacktics(), function.Description, glyph, _functionIndex, context.Session);
                            completions.Add(completion);
                        }
                    }
                }
            }

            return completions;
        }
        #endregion

        #region IRHelpSearchTermProvider
        public IReadOnlyCollection<string> GetEntries() {
            var list = new List<string>();
            foreach (IPackageInfo pkg in _packageIndex.Packages) {
                list.AddRange(pkg.Functions.Select(x => x.Name));
            }
            return list;
        }
        #endregion

        private IEnumerable<IPackageInfo> GetPackages(IRCompletionContext context) {
            if (context.IsCaretInNameSpace()) {
                return GetSpecificPackage(context);
            }

            var t = GetAllFilePackagesAsync(context);
            t.Wait(_asyncWaitTimeout);
            return t.IsCompleted ? t.Result : Enumerable.Empty<IPackageInfo>();
        }

        /// <summary>
        /// Retrieves name of the package in 'package::' statement
        /// so intellisense can show list of functions available
        /// in the specific package.
        /// </summary>
        private IEnumerable<IPackageInfo> GetSpecificPackage(IRCompletionContext context) {
            var packages = new List<IPackageInfo>();
            var snapshot = context.EditorBuffer.CurrentSnapshot;
            int colons = 0;

            for (int i = context.Position - 1; i >= 0; i--, colons++) {
                char ch = snapshot[i];
                if (ch != ':') {
                    break;
                }
            }

            if (colons > 1 && colons < 4) {
                string packageName = string.Empty;
                int start = 0;
                int end = context.Position - colons;

                for (int i = end - 1; i >= 0; i--) {
                    char ch = snapshot[i];
                    if (!RTokenizer.IsIdentifierCharacter(ch)) {
                        start = i + 1;
                        break;
                    }
                }

                if (start < end) {
                    packageName = snapshot.GetText(TextRange.FromBounds(start, end));
                    if (packageName.Length > 0) {
                        context.InternalFunctions = colons == 3;
                        var package = GetPackageByName(packageName);
                        if (package != null) {
                            packages.Add(package);
                        }
                    }
                }
            }

            return packages;
        }

        /// <summary>
        /// Retrieves list of packages declared in the file via 'library' statements
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task<IEnumerable<IPackageInfo>> GetAllFilePackagesAsync(IRCompletionContext context) {

            IEnumerable<string> loadedPackages = _session?.LoadedPackageNames ?? Enumerable.Empty<string>();
            IEnumerable<string> filePackageNames = context.AstRoot.GetFilePackageNames();
            IEnumerable<string> allPackageNames = PackageIndex.PreloadedPackages.Union(filePackageNames).Union(loadedPackages);

            return _packageIndex.GetPackagesInfoAsync(allPackageNames);
        }

        private IPackageInfo GetPackageByName(string packageName) {
            var t = _packageIndex.GetPackageInfoAsync(packageName);
            t.Wait(_asyncWaitTimeout);
            return t.IsCompleted ? t.Result : null;
        }
    }
}
