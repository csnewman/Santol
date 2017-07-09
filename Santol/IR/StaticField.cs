using System;
using LLVMSharp;
using Santol.Generator;
using Santol.Loader;

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

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            LLVMTypeRef type = Type.GetType(codeGenerator);
            LLVMValueRef val = codeGenerator.GetGlobal(MangledName, type);
            LLVM.SetInitializer(val, LLVM.ConstNull(type));
            LLVM.SetLinkage(val, LLVMLinkage.LLVMExternalLinkage);
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetGlobal(MangledName, Type.GetType(codeGenerator));
        }
    }
}