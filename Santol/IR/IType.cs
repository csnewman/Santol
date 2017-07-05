using LLVMSharp;
using Santol.Generator;

namespace Santol.IR
{
    public interface IType
    {
        string Name { get; }
        string MangledName { get; }

        LLVMTypeRef GetType(CodeGenerator codeGenerator);

        LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value);
    }
}