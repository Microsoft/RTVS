// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Shell;
using Microsoft.UnitTests.Core.Mef;

namespace Microsoft.Languages.Editor.Test.Shell {
    /// <summary>
    /// Composition catalog that is primarily used in interactive tests.
    /// It is assigned to shell.Current.CompositionService.
    /// In interactive tests catalog that also includes host application
    /// objects such as VS components is not suitable as it may be exporting
    /// objects that cannot be instantiated in a limited test environment.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EditorTestCompositionCatalog : ICompositionCatalog {
        /// <summary>
        /// Instance of the compostion catalog to use in editor tests.
        /// It should not be used in the app/package level tests.
        /// </summary>
        private static Lazy<EditorTestCompositionCatalog> _instance = Lazy.Create(() => new EditorTestCompositionCatalog());
        private static readonly object _containerLock = new object();

        /// <summary>
        /// MEF container of this instance. Note that there may be more
        /// than one container in test runs. For example, editor tests
        /// just the editor-levle container that does not have objects
        /// exported from the package. Package tests use bigger container
        /// that also includes objects exported from package-level assemblies.
        /// </summary>
        private CompositionContainer _container;

        private static bool _traceExportImports = false;

        /// <summary>
        /// Assemblies used at the R editor level
        /// </summary>
        private static string[] _rtvsEditorAssemblies = {
            "Microsoft.Markdown.Editor.dll",
            "Microsoft.Languages.Editor.dll",
            "Microsoft.Languages.Editor.Windows.dll",
            "Microsoft.Languages.Editor.Application.dll",
            "Microsoft.R.Editor.dll",
            "Microsoft.R.Editor.Test.dll",
            "Microsoft.R.Support.dll",
            "Microsoft.R.Support.Test.dll",
            "Microsoft.R.Components.dll",
            "Microsoft.R.Components.Windows.dll",
            "Microsoft.R.Components.Test.dll",
            "Microsoft.R.Common.Core.dll",
            "Microsoft.R.Host.Client.Windows.dll",
            "Microsoft.R.Debugger.dll",
        };

        /// <summary>
        /// Assemblies of the VS core text editor
        /// </summary>
        private static string[] _coreEditorAssemblies = {
            "Microsoft.VisualStudio.CoreUtility.dll",
            "Microsoft.VisualStudio.Editor.dll",
            "Microsoft.VisualStudio.Language.Intellisense.dll",
            "Microsoft.VisualStudio.Platform.VSEditor.dll",
            "Microsoft.VisualStudio.Text.Data.dll",
            "Microsoft.VisualStudio.Text.Logic.dll",
            "Microsoft.VisualStudio.Text.UI.dll",
            "Microsoft.VisualStudio.Text.UI.Wpf.dll",
        };

        private static string[] _privateEditorAssemblies = {
            "Microsoft.VisualStudio.Platform.VSEditor.Interop.dll"
        };

        /// <summary>
        /// VS project system assemblies
        /// </summary>
        private static string[] _projectAssemblies = {
            "Microsoft.VisualStudio.ProjectSystem.dll",
            "Microsoft.VisualStudio.ProjectSystem.VS.dll",
            "Microsoft.VisualStudio.ProjectSystem.Implementation.dll",
            "Microsoft.VisualStudio.ProjectSystem.VS.Implementation.dll"
         };

        /// <summary>
        /// Additional assemblies supplied by the creator class
        /// </summary>
        private static string[] _additionalAssemblies = new string[0];

        /// <summary>
        /// Instance of the compostion catalog to use in editor tests.
        /// It should not be used in the app/package level tests.
        /// </summary>
        public static ICompositionCatalog Current => _instance.Value;

        /// <summary>
        /// Only used if catalog is created as part of a bigger catalog
        /// such as when package-level tests supply additional assemblies.
        /// </summary>
        /// <param name="additionalAssemblies"></param>
        public EditorTestCompositionCatalog(string[] additionalAssemblies) {
            _additionalAssemblies = additionalAssemblies;
            _container = CreateContainer();
        }

        private EditorTestCompositionCatalog() {
            _container = CreateContainer();
        }

        private CompositionContainer CreateContainer() {
            lock (_containerLock) {
                CompositionContainer container;

                string thisAssembly = Assembly.GetExecutingAssembly().GetAssemblyPath();
                string assemblyLoc = Path.GetDirectoryName(thisAssembly);

                var assemblies = new List<string>();
                assemblies.AddRange(_coreEditorAssemblies);
                assemblies.AddRange(_projectAssemblies);
                assemblies.AddRange(_privateEditorAssemblies);
                assemblies.AddRange(_rtvsEditorAssemblies);
                assemblies.AddRange(_additionalAssemblies);

                var aggregateCatalog = CatalogFactory.CreateAssembliesCatalog(assemblies);
                AssemblyCatalog thisAssemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
                aggregateCatalog.Catalogs.Add(thisAssemblyCatalog);

                var filteredCatalog = aggregateCatalog.Filter(cpd => !cpd.ContainsPartMetadataWithKey(PartMetadataAttributeNames.SkipInEditorTestCompositionCatalog));
                container = BuildCatalog(filteredCatalog);
                return container;
            }
        }

        private CompositionContainer BuildCatalog(ComposablePartCatalog aggregateCatalog) {
            CompositionContainer container = new CompositionContainer(aggregateCatalog, isThreadSafe: true);
            container.ComposeParts(container.Catalog.Parts.AsEnumerable());

            TraceExportImports(container);
            return container;
        }

        private void TraceExportImports(CompositionContainer container) {
            if (!_traceExportImports) {
                return;
            }
            var parts = new StringBuilder();
            var exports = new StringBuilder();

            foreach (object o in container.Catalog.Parts) {
                ComposablePartDefinition part = o as ComposablePartDefinition;
                if (part == null) {
                    parts.AppendLine("PART MISSING: " + o.ToString());
                    exports.AppendLine("PART MISSING: " + o.ToString());
                    continue;
                }

                parts.AppendLine("===============================================================");
                parts.AppendLine(part.ToString());

                exports.AppendLine("===============================================================");
                exports.AppendLine(part.ToString());

                bool first = true;

                if (part.ExportDefinitions.Any()) {
                    parts.AppendLine("\t --- EXPORTS --");
                    exports.AppendLine("\t --- EXPORTS --");

                    foreach (ExportDefinition exportDefinition in part.ExportDefinitions) {
                        parts.AppendLine("\t" + exportDefinition.ContractName);
                        exports.AppendLine("\t" + exportDefinition.ContractName);

                        foreach (KeyValuePair<string, object> kvp in exportDefinition.Metadata) {
                            string valueString = kvp.Value != null ? kvp.Value.ToString() : string.Empty;

                            parts.AppendLine("\t" + kvp.Key + " : " + valueString);
                            exports.AppendLine("\t" + kvp.Key + " : " + valueString);
                        }

                        if (first) {
                            first = false;
                        } else {
                            parts.AppendLine("------------------------------------------------------");
                            exports.AppendLine("------------------------------------------------------");
                        }
                    }
                }

                if (part.ImportDefinitions.Any()) {
                    parts.AppendLine("\t --- IMPORTS ---");

                    foreach (ImportDefinition importDefinition in part.ImportDefinitions) {
                        parts.AppendLine("\t" + importDefinition.ContractName);
                        parts.AppendLine("\t" + importDefinition.Constraint.ToString());
                        parts.AppendLine("\t" + importDefinition.Cardinality.ToString());

                        if (first) {
                            first = false;
                        } else {
                            parts.AppendLine("------------------------------------------------------");
                        }
                    }
                }
            }

            WriteTraceToFile(parts.ToString(), "Parts.txt");
            WriteTraceToFile(exports.ToString(), "Exports.txt");
        }

        private void WriteTraceToFile(string s, string fileName) {
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            File.Delete(filePath);

            using (var sw = new StreamWriter(filePath)) {
                sw.Write(s);
            }
        }

#region ICompositionCatalog
        public ICompositionService CompositionService => _container;

        public ExportProvider ExportProvider => _container;

        public CompositionContainer Container => _container;
#endregion
    }
}
