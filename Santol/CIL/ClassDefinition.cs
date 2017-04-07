using System.Collections.Generic;
using Mono.Cecil;

namespace Santol.CIL
{
    public class ClassDefinition
    {
        public string Name { get; }
        public string Namespace { get; }
        public string FullName { get; }
        public ModuleDefinition Module { get; }
        public IList<MethodDefinition> Methods { get; }

        public ClassDefinition(string name, string @namespace, string fullName, ModuleDefinition module)
        {
            Name = name;
            Namespace = @namespace;
            FullName = fullName;
            Methods = new List<MethodDefinition>();
            Module = module;
        }

        public void AddMethod(MethodDefinition method)
        {
            Methods.Add(method);
        }
    }
}