using System;
using System.Collections.Generic;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.IR
{
    public class ClassType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        private IList<IField> _fields;
        private IList<IMethod> _methods;

        public ClassType(string name)
        {
            Name = name;
            MangledName = $"C_{name.Replace('.', '_').Replace("*", "PTR")}";
            _fields = new List<IField>();
            _methods = new List<IMethod>();
        }

        public void AddField(IField field)
        {
            _fields.Add(field);
        }

        public void AddMethod(IMethod method)
        {
            _methods.Add(method);
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value)
        {
            throw new NotSupportedException("Constant classes not supported");
        }

        public LLVMValueRef? ConvertTo(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef? ConvertFrom(CodeGenerator codeGenerator, IType type, LLVMValueRef value)
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

        public IMethod ResolveMethod(MethodReference method)
        {
            throw new NotImplementedException();
        }

        public void Generate(CodeGenerator codeGenerator)
        {
            foreach (IField field in _fields)
                field.Generate(codeGenerator);


//            throw new NotImplementedException();
        }
    }
}