using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Santol.CIL
{
    public class EnumDefinition : ITypeDefinition
    {
        public string Name { get; }
        public string Namespace { get; }
        public string FullName { get; }
        public ModuleDefinition Module { get; }
        public TypeReference Type { get; }
        public IDictionary<string, object> Values { get; }

        public EnumDefinition(string name, string @namespace, string fullName, ModuleDefinition module, TypeReference type, IDictionary<string, object> values)
        {
            Name = name;
            Namespace = @namespace;
            FullName = fullName;
            Module = module;
            Type = type;
            Values = values;
        }
    }
}