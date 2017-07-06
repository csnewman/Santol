using System;
using LLVMSharp;
using Santol.Generator;

namespace Santol.IR
{
    public class StaticField : IField
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType Type { get; }
        public bool IsShared => true;

        public StaticField(IType parent, IType type, string name)
        {
            Parent = parent;
            Name = name;
            MangledName = $"{parent.MangledName}_SF_{type.MangledName}_{name}";
            Type = type;
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }
    }
}