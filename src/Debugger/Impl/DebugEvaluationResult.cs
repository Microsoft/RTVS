﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace Microsoft.R.Debugger {
    [Flags]
    public enum DebugEvaluationResultFields : ulong {
        None,
        Expression = 1 << 1,
        Kind = 1 << 2,
        Repr = 1 << 3,
        ReprDPut = 1 << 4,
        ReprToString = 1 << 5,
        ReprStr = 1 << 6,
        ReprAll = Repr | ReprDPut | ReprStr | ReprToString,
        TypeName = 1 << 7,
        Classes = 1 << 8,
        Length = 1 << 9,
        SlotCount = 1 << 10,
        AttrCount = 1 << 11,
        NameCount = 1 << 12,
        Dim = 1 << 13,
        EnvName = 1 << 14,
        Flags = 1 << 15,
        All = ulong.MaxValue,
    }

    internal static class DebugEvaluationResultFieldsExtensions {
        private static readonly Dictionary<DebugEvaluationResultFields, string> _mapping = new Dictionary<DebugEvaluationResultFields, string> {
            [DebugEvaluationResultFields.Expression] = "expression",
            [DebugEvaluationResultFields.Kind] = "kind",
            [DebugEvaluationResultFields.Repr] = "repr",
            [DebugEvaluationResultFields.ReprDPut] = "repr.dput",
            [DebugEvaluationResultFields.ReprToString] = "repr.toString",
            [DebugEvaluationResultFields.ReprStr] = "repr.str",
            [DebugEvaluationResultFields.TypeName] = "type",
            [DebugEvaluationResultFields.Classes] = "classes",
            [DebugEvaluationResultFields.Length] = "length",
            [DebugEvaluationResultFields.SlotCount] = "slot_count",
            [DebugEvaluationResultFields.AttrCount] = "attr_count",
            [DebugEvaluationResultFields.NameCount] = "name_count",
            [DebugEvaluationResultFields.Dim] = "dim",
            [DebugEvaluationResultFields.EnvName] = "env_name",
            [DebugEvaluationResultFields.Flags] = "flags",
        };

        public static string ToRVector(this DebugEvaluationResultFields fields) {
            if (fields == DebugEvaluationResultFields.All) {
                return null;
            }

            var fieldNames = _mapping.Where(kv => fields.HasFlag(kv.Key)).Select(kv => "'" + kv.Value + "'");
            return Invariant($"c({string.Join(", ", fieldNames)})");
        }
    }

    public abstract class DebugEvaluationResult {
        public DebugStackFrame StackFrame { get; }
        public string Expression { get; }
        public string Name { get; }

        internal DebugEvaluationResult(DebugStackFrame stackFrame, string expression, string name) {
            StackFrame = stackFrame;
            Expression = expression;
            Name = name;
        }

        internal static DebugEvaluationResult Parse(DebugStackFrame stackFrame, string name, JObject json) {
            var expression = json.Value<string>("expression");

            var errorText = json.Value<string>("error");
            if (errorText != null) {
                return new DebugErrorEvaluationResult(stackFrame, expression, name, errorText);
            }

            var code = json.Value<string>("promise");
            if (code != null) {
                return new DebugPromiseEvaluationResult(stackFrame, expression, name, code);
            }

            var isActiveBinding = json.Value<bool?>("active_binding");
            if (isActiveBinding == true) {
                return new DebugActiveBindingEvaluationResult(stackFrame, expression, name);
            }

            return new DebugValueEvaluationResult(stackFrame, expression, name, json);
        }

        public Task<DebugEvaluationResult> SetValueAsync(string value) {
            return StackFrame.EvaluateAsync(Invariant($"{Expression} <- {value}"), reprMaxLength: 0);
        }
    }

    public class DebugErrorEvaluationResult : DebugEvaluationResult {
        public string ErrorText { get; }

        public DebugErrorEvaluationResult(DebugStackFrame stackFrame, string expression, string name, string errorText)
            : base(stackFrame, expression, name) {
            ErrorText = errorText;
        }

        public override string ToString() {
            return Invariant($"ERROR: {ErrorText}");
        }
    }

    public enum DebugValueEvaluationResultKind {
        UnnamedItem,
        NamedItem,
        Slot,
    }

    [Flags]
    public enum DebugValueEvaluationResultFlags {
        None,
        Atomic = 1 << 1,
        Recursive = 1 << 2,
        HasParentEnvironment = 1 << 3,
    }

    public struct DebugValueEvaluationResultRepresentation {
        public readonly string DPut;
        public readonly new string ToString;
        public readonly string Str;

        public DebugValueEvaluationResultRepresentation(JObject repr) {
            DPut = repr.Value<string>("dput");
            ToString = repr.Value<string>("toString");
            Str = repr.Value<string>("str");
        }
    }

    public class DebugValueEvaluationResult : DebugEvaluationResult {
        public DebugValueEvaluationResultKind Kind { get; }
        public DebugValueEvaluationResultRepresentation Representation { get; }
        public string TypeName { get; }
        public IReadOnlyList<string> Classes { get; }
        public int? Length { get; }
        public int? AttributeCount { get; }
        public int? SlotCount { get; }
        public int? NameCount { get; }
        public DebugValueEvaluationResultFlags Flags { get; }

        public bool IsAtomic => Flags.HasFlag(DebugValueEvaluationResultFlags.Atomic);
        public bool IsRecursive => Flags.HasFlag(DebugValueEvaluationResultFlags.Recursive);
        public bool HasAttributes => AttributeCount != 0;
        public bool HasSlots => SlotCount != 0;
        public bool HasChildren => HasSlots || Length > (IsAtomic || TypeName == "closure" ? 1 : 0);

        internal DebugValueEvaluationResult(DebugStackFrame stackFrame, string expression, string name, JObject json)
            : base(stackFrame, expression, name) {

            var repr = json["repr"];
            if (repr != null) {
                var reprObj = repr as JObject;
                if (reprObj == null) {
                    throw new InvalidDataException(Invariant($"'repr' must be an object in:\n\n{json}"));
                }
                Representation = new DebugValueEvaluationResultRepresentation(reprObj);
            }

            TypeName = json.Value<string>("type");
            Length = json.Value<int?>("length");
            AttributeCount = json.Value<int?>("attr_count");
            SlotCount = json.Value<int?>("slot_count");
            NameCount = json.Value<int?>("name_count");

            var classes = json.Value<JArray>("classes");
            if (classes != null) {
                Classes = classes.Select(t => t.Value<string>()).ToArray();
            }

            var kind = json.Value<string>("kind");
            switch (kind) {
                case null:
                case "[[":
                    Kind = DebugValueEvaluationResultKind.UnnamedItem;
                    break;
                case "$":
                    Kind = DebugValueEvaluationResultKind.NamedItem;
                    break;
                case "@":
                    Kind = DebugValueEvaluationResultKind.Slot;
                    break;
                default:
                    throw new InvalidDataException(Invariant($"Invalid kind '{kind}' in:\n\n{json}"));
            }

            var flags = json.Value<JArray>("flags").Select(v => v.Value<string>());
            foreach (var flag in flags) {
                switch (flag) {
                    case "atomic":
                        Flags |= DebugValueEvaluationResultFlags.Atomic;
                        break;
                    case "recursive":
                        Flags |= DebugValueEvaluationResultFlags.Recursive;
                        break;
                    case "has_parent_env":
                        Flags |= DebugValueEvaluationResultFlags.HasParentEnvironment;
                        break;
                    default:
                        throw new InvalidDataException(Invariant($"Unrecognized flag '{flag}' in:\n\n{json}"));
                }
            }
        }

        public async Task<IReadOnlyList<DebugEvaluationResult>> GetChildrenAsync(
            DebugEvaluationResultFields fields = DebugEvaluationResultFields.All,
            int? maxLength = null,
            int? reprMaxLength = null
        ) {
            await TaskUtilities.SwitchToBackgroundThread();

            if (StackFrame == null) {
                throw new InvalidOperationException("Cannot retrieve children of an evaluation result that is not tied to a frame.");
            }

            var call = Invariant($@"rtvs:::toJSON(rtvs:::describe_children(
                {Expression.ToRStringLiteral()}, {StackFrame.SysFrame}, 
                {fields.ToRVector()}, {maxLength}, {reprMaxLength}))");
            var jChildren = await StackFrame.Session.InvokeDebugHelperAsync<JArray>(call);
            Trace.Assert(
                jChildren.Children().All(t => t is JObject),
                Invariant($"rtvs:::describe_children(): object of objects expected.\n\n{jChildren}"));

            var children = new List<DebugEvaluationResult>();
            foreach (var child in jChildren) {
                var childObject = (JObject)child;
                Trace.Assert(
                    childObject.Count == 1,
                    Invariant($"rtvs:::describe_children(): each object is expected contain one object\n\n"));
                foreach (var kv in childObject) {
                    var name = kv.Key;
                    var jEvalResult = (JObject)kv.Value;
                    var evalResult = Parse(StackFrame, name, jEvalResult);
                    children.Add(evalResult);
                }
            }

            return children;
        }

        public override string ToString() {
            return Invariant($"VALUE: {TypeName} {Representation.DPut}");
        }
    }

    public class DebugPromiseEvaluationResult : DebugEvaluationResult {
        public string Code { get; }

        public DebugPromiseEvaluationResult(DebugStackFrame stackFrame, string expression, string name, string code)
            : base(stackFrame, expression, name) {
            Code = code;
        }

        public override string ToString() {
            return Invariant($"PROMISE: {Code}");
        }
    }

    public class DebugActiveBindingEvaluationResult : DebugEvaluationResult {
        public DebugActiveBindingEvaluationResult(DebugStackFrame stackFrame, string expression, string name)
            : base(stackFrame, expression, name) {
        }
    }
}
