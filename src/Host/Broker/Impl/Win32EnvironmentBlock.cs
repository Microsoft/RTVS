﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.R.Host.Broker {
    public class Win32EnvironmentBlock : Dictionary<string, string> {
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        public static Win32EnvironmentBlock Create(IntPtr token, bool inherit = false) {
            IntPtr env = IntPtr.Zero;
            Win32EnvironmentBlock eb = new Win32EnvironmentBlock();
            try {
                if (CreateEnvironmentBlock(out env, token, inherit)) {
                    IntPtr ptr = env;
                    while (true) {
                        string envVar = Marshal.PtrToStringUni(ptr);
                        byte[] data = Encoding.Unicode.GetBytes(envVar);
                        if (data.Length <= 2) {
                            // detected double null
                            break;
                        } else {
                            int idx = envVar.IndexOf('=');
                            if (idx > 0 && idx < envVar.Length) {
                                string key = envVar.Substring(0, idx);
                                string value = envVar.Substring(idx + 1);
                                eb.Add(key, value);
                            }
                        }
                        // unicode string + unicode null
                        ptr += data.Length + 2;
                    }
                }
            } finally {
                DestroyEnvironmentBlock(env);
            }
            return eb;
        }

        public byte[] ToByteArray() {
            using (MemoryStream ms = new MemoryStream()) {
                byte[] nulls = { 0, 0 };
                foreach (var p in this) {
                    string envData = $"{p.Key}={p.Value}";
                    byte[] data = Encoding.Unicode.GetBytes(envData);
                    ms.Write(data, 0, data.Length);
                    ms.Write(nulls, 0, nulls.Length);
                }
                ms.Write(nulls, 0, nulls.Length);
                return ms.ToArray();
            }
        }

        public IntPtr ToPtr() {
            int length;
            return ToPtr(out length);
        }

        public IntPtr ToPtr(out int length) {
            byte[] data = ToByteArray();
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data, 0, ptr, data.Length);

            length = data.Length;
            return ptr;
        }

        public new string this[string key] {
            get {
                if (ContainsKey(key)) {
                    return base[key];
                } else {
                    return null;
                }
            }
            set {
                if (ContainsKey(key)) {
                    base[key] = value;
                } else {
                    Add(key, value);
                }
            }
        }

    }
}
