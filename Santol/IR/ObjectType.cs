using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class ObjectType : IType
    {
        public static readonly ObjectType Instance = new ObjectType();

        public string Name => "System.Object";
        public string MangledName => "object";
        public bool IsAllowedOnStack => false;
        public bool IsPointer => false;
        public TypeInfo TypeInfo { get; }

        private ObjectType()
        {
            TypeInfo = new TypeInfo(MangledName, null);
        }

        public IType GetLocalReferenceType()
        {
            throw new NotImplementedException();
        }

        public IType GetStackType()
        {
            throw new NotImplementedException();
        }

        public bool IsInHierarchy(IType type)
        {
            return false;
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetStruct(MangledName, type =>
            {
                IList<LLVMTypeRef> types = new List<LLVMTypeRef>();
                types.Add(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0));
                LLVM.StructSetBody(type, types.ToArray(), false);
            });
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
            throw new NotImplementedException();
        }

        public LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            throw new NotImplementedException();
        }

        public bool IsStackCompatible(IType other)
        {
            throw new NotImplementedException();
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
            if (method.Name.Equals(".ctor"))
                return FakeMethod.NoAction;

            throw new NotImplementedException();
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }
    }
}