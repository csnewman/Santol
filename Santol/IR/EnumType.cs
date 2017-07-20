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
    public class EnumType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        public bool IsAllowedOnStack => true;
        public bool IsPointer => false;
        public TypeInfo TypeInfo => throw new NotImplementedException();
        public IType UnderlyingType { get; }
        private IList<ConstantField> _fields;

        public EnumType(string name, IType underlyingType)
        {
            Name = name;
            MangledName = $"E_{name.Replace('.', '_')}";
            UnderlyingType = underlyingType;
            _fields = new List<ConstantField>();
        }

        public void AddField(ConstantField constant)
        {
            _fields.Add(constant);
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
            return UnderlyingType.GetType(codeGenerator);
        }

        public LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value)
        {
            return UnderlyingType.GenerateConstantValue(codeGenerator, value);
        }

        public void LoadDefault(CodeGenerator codeGenerator, LLVMValueRef target)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef? ConvertTo(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            return type.Equals(UnderlyingType) ? value : UnderlyingType.ConvertTo(codeGenerator, type, value);
        }

        public LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            return type.Equals(UnderlyingType) ? value : UnderlyingType.ConvertFrom(codeGenerator, type, value);
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
            throw new NotSupportedException("Enums can not have fields");
        }

        public LLVMValueRef GetTypeInfoField(CodeGenerator codeGenerator, LLVMValueRef objectPtr)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField field)
        {
            throw new NotSupportedException("Enums can not have fields");
        }

        public LLVMValueRef ExtractField(CodeGenerator codeGenerator, LLVMValueRef objectRef, IField field)
        {
            throw new NotImplementedException();
        }

        public IMethod ResolveMethod(AssemblyLoader assemblyLoader, MethodReference method)
        {
            throw new NotSupportedException("Enums can not have methods");
        }

        public IMethod FindMethodImplementation(IMethod method)
        {
            throw new NotImplementedException();
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            foreach (ConstantField constantField in _fields)
                constantField.Generate(assemblyLoader, codeGenerator);
        }
    }
}