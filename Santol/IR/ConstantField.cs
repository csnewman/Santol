using LLVMSharp;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class ConstantField : IField
    {
        public IType Parent { get; }
        public string Name { get; }
        public string MangledName { get; }
        public IType Type { get; }
        public bool IsShared => true;
        public object Value { get; set; }

        public ConstantField(IType parent, IType type, string name, object value)
        {
            Parent = parent;
            Name = name;
            MangledName = $"{parent.MangledName}_CF_{type.MangledName}_{name}";
            Type = type;
            Value = value;
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            LLVMTypeRef type = Type.GetType(codeGenerator);
            LLVMValueRef val = codeGenerator.GetGlobal(MangledName, type);
            LLVM.SetInitializer(val, Type.GenerateConstantValue(codeGenerator, Value));
            LLVM.SetLinkage(val, LLVMLinkage.LLVMExternalLinkage);
            LLVM.SetGlobalConstant(val, true);
        }
    }
}