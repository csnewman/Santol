using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class LocalField : IField
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType Type { get; }
        public bool IsShared => false;

        public LocalField(IType parent, IType type, string name)
        {
            Parent = parent;
            Name = name;
            MangledName = $"{parent.MangledName}_LF_{type.MangledName}_{name}";
            Type = type;
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
        }
    }
}