using System.Collections.Generic;

namespace Santol
{
    public class ClassDefinition
    {
        public string Name { get; }
        public string Namespace { get; }
        public string FullName { get; }
        public IList<MethodDefinition> Methods { get; }

        public ClassDefinition(string name, string @namespace, string fullName)
        {
            Name = name;
            Namespace = @namespace;
            FullName = fullName;
            Methods = new List<MethodDefinition>();
        }

        public void AddMethod(MethodDefinition method)
        {
            Methods.Add(method);
        }
    }
}