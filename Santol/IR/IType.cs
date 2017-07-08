using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.IR
{
    public interface IType
    {
        string Name { get; }
        string MangledName { get; }

        LLVMTypeRef GetType(CodeGenerator codeGenerator);

        LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value);

        LLVMValueRef? ConvertTo(CodeGenerator codeGenerator, IType type, LLVMValueRef value);

        LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value);

        IType GetMostComplexType(IType other);

        IField ResolveField(FieldReference field);

        LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField field);

    }
}