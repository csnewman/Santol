using LLVMSharp;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public interface IMethod
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        bool IsStatic { get; }
        bool IsLocal { get; }
        bool ImplicitThis { get; }
        IType ReturnType { get; }
        IType[] Arguments { get; }

        void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator);

        LLVMTypeRef GetMethodType(CodeGenerator codeGenerator);

        LLVMValueRef? GenerateCall(CodeGenerator codeGenerator, LLVMValueRef[] arguments);
    }
}