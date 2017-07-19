using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public interface IType
    {
        string Name { get; }
        string MangledName { get; }
        bool IsAllowedOnStack { get; }
        bool IsPointer { get; }
        TypeInfo TypeInfo { get; }

        IType GetLocalReferenceType();

        IType GetStackType();

        bool IsInHierarchy(IType type);

        LLVMTypeRef GetType(CodeGenerator codeGenerator);

        LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value);

        void LoadDefault(CodeGenerator codeGenerator, LLVMValueRef target);

        LLVMValueRef? ConvertTo(CodeGenerator codeGenerator, IType type, LLVMValueRef value);

        LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value);

        bool IsStackCompatible(IType other);

        IType GetMostComplexType(IType other);

        IField ResolveField(FieldReference field);

        LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField field);

        LLVMValueRef ExtractField(CodeGenerator codeGenerator, LLVMValueRef objectRef, IField field);

        IMethod ResolveMethod(AssemblyLoader assemblyLoader, MethodReference method);

        void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator);
    }
}