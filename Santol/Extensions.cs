using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Santol
{
    public static class Extensions
    {
        public static string ToNiceString(this TypeReference type)
        {
            string extra = (type.IsValueType ? " val" : "") + (type.IsArray ? " array" : "") +
                           (type.IsByReference ? " ref" : "") + (type.IsFunctionPointer ? " fncptr" : "") +
                           (type.IsGenericInstance ? " geninst" : "") + (type.IsGenericParameter ? " genparam" : "") +
                           (type.IsNested ? " nested" : "") + (type.IsOptionalModifier ? " opt" : "") +
                           (type.IsPinned ? " pinned" : "") + (type.IsPointer ? " ptr" : "") +
                           (type.IsPrimitive ? " prim" : "");
            return $"{type.FullName}{(type.HasGenericParameters ? "<type>" : "")}({type.MetadataType}{extra})";
        }

        public static string GetName(this TypeReference definition)
        {
            return definition.FullName.Replace('.', '_');
        }

        public static string GetName(this Mono.Cecil.MethodDefinition definition)
        {
            return definition.DeclaringType.GetName() + "____" + definition.Name + "___" +
                   string.Join("__", definition.Parameters.Select(p => p.ParameterType.GetName())) + "___" +
                   definition.ReturnType.GetName();
        }

        public static void AddOpt<T>(this IList<T> list, T value)
        {
            if (!list.Contains(value))
                list.Add(value);
        }

        public static void AddOpt<T>(this IList<T> list, T[] values)
        {
            foreach (T value in values)
                list.AddOpt(value);
        }
    }
}