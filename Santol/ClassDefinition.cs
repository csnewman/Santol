using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Santol
{
    public class ClassDefinition
    {
        public string Name { get; }
        public string Namespace { get; }
        public string FullName { get; }

        public ClassDefinition(string name, string @namespace, string fullName)
        {
            Name = name;
            Namespace = @namespace;
            FullName = fullName;
        }
    }
}