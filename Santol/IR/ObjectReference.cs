using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class ObjectReference : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        public bool IsAllowedOnStack => true;
        public bool IsPointer => true;
        public TypeInfo TypeInfo => throw new NotImplementedException();
        public IType Target { get; }

        public ObjectReference(IType target)
        {
            Name = $"{target.Name}*";
            MangledName = $"_{target.MangledName}_OPTR";
            Target = target;
        }

        public IType GetLocalReferenceType()
        {
            return this;
        }

        public IType GetStackType()
        {
            return this;
        }

        public bool IsInHierarchy(IType type)
        {
            throw new NotImplementedException();
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return LLVM.PointerType(Target.GetType(codeGenerator), 0);
        }

        public LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value)
        {
            throw new NotImplementedException();
        }

        public void LoadDefault(CodeGenerator codeGenerator, LLVMValueRef target)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef? ConvertTo(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            ObjectReference other = type as ObjectReference;
            if (other == null)
                return null;
            if (!Target.IsInHierarchy(other.Target))
                return null;
            return LLVM.BuildBitCast(codeGenerator.Builder, value, type.GetType(codeGenerator), "");
        }

        public LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            ObjectReference other = type as ObjectReference;
            if (other == null)
                return null;
            if (!other.Target.IsInHierarchy(Target))
                return null;
            return LLVM.BuildBitCast(codeGenerator.Builder, value, GetType(codeGenerator), "");
        }

        public bool IsStackCompatible(IType other)
        {
            return Equals(other);
        }

        public IType GetMostComplexType(IType other)
        {
            throw new NotImplementedException();
        }

        public IField ResolveField(FieldReference field)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField field)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef ExtractField(CodeGenerator codeGenerator, LLVMValueRef objectRef, IField field)
        {
            throw new NotImplementedException();
        }

        public IMethod ResolveMethod(AssemblyLoader assemblyLoader, MethodReference method)
        {
            throw new NotImplementedException();
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }
    }
}