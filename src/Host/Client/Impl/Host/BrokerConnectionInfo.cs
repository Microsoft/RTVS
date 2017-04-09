// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Common.Core;
using Microsoft.Common.Core.Security;

namespace Microsoft.R.Host.Client.Host {
    [DebuggerDisplay("{Uri}, IsRemote={IsRemote}, InterpreterId={InterpreterId}")]
    public struct BrokerConnectionInfo {
        public string Name { get; }
        public Uri Uri { get; }
        public bool IsValid { get; }
        public bool IsRemote { get; }
        public string ParametersId { get; }
        public string RCommandLineArguments { get; }
        public string InterpreterId { get; }
        public string CredentialAuthority => GetCredentialAuthority(Name);

        public static BrokerConnectionInfo Create(ISecurityService securityService, string name, string path, string rCommandLineArguments = null) {
            rCommandLineArguments = rCommandLineArguments ?? string.Empty;

            Uri uri;
            if (!Uri.TryCreate(path, UriKind.Absolute, out uri)) {
                return new BrokerConnectionInfo();
            }

            return uri.IsFile 
                ? new BrokerConnectionInfo(name, uri, rCommandLineArguments, string.Empty, false, string.Empty) 
                : CreateRemote(name, uri, securityService, rCommandLineArguments);
        }

        private static BrokerConnectionInfo CreateRemote(string name, Uri uri, ISecurityService securityService, string rCommandLineArguments) {
            var fragment = uri.Fragment;
            var interpreterId = string.IsNullOrEmpty(fragment) ? string.Empty : fragment.Substring(1);
            uri = new Uri(uri.GetLeftPart(UriPartial.Query));
            string username = securityService.GetUserName(GetCredentialAuthority(name));
            return new BrokerConnectionInfo(name, uri, rCommandLineArguments, interpreterId, true, username);
        }

        private BrokerConnectionInfo(string name, Uri uri, string rCommandLineArguments, string interpreterId, bool isRemote, string username) {
            Name = name;
            IsValid = true;
            Uri = uri;
            RCommandLineArguments = rCommandLineArguments?.Trim() ?? string.Empty;
            InterpreterId = interpreterId;
            ParametersId = string.IsNullOrEmpty(rCommandLineArguments) && string.IsNullOrEmpty(interpreterId) && string.IsNullOrEmpty(username)
                ? string.Empty 
                : $"{rCommandLineArguments}/{interpreterId}/{username}".GetSHA256FileSystemSafeHash();
            IsRemote = isRemote;
        }

        private static string GetCredentialAuthority(string name) {
            return $"RTVS:{name}";
        }

        public override bool Equals(object obj) => obj is BrokerConnectionInfo && Equals((BrokerConnectionInfo)obj);

        public bool Equals(BrokerConnectionInfo other) => other.ParametersId.EqualsOrdinal(ParametersId) && Equals(other.Uri, Uri);

        public override int GetHashCode() {
            unchecked {
                return ((ParametersId?.GetHashCode() ?? 0)*397) ^ (Uri != null ? Uri.GetHashCode() : 0);
            }
        }

        public static bool operator ==(BrokerConnectionInfo a, BrokerConnectionInfo b) => a.Equals(b);

        public static bool operator !=(BrokerConnectionInfo a, BrokerConnectionInfo b) => !a.Equals(b);
    }
}