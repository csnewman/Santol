﻿using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class ClassType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        public bool IsAllowedOnStack => false;
        public bool IsPointer => false;
        private IDictionary<FieldReference, IField> _fields;
        private IDictionary<MethodReference, IMethod> _methods;
        private IType _localReferenceType;

        public ClassType(string name)
        {
            Name = name;
            MangledName = $"C_{name.Replace('.', '_')}";
            _fields = new Dictionary<FieldReference, IField>();
            _methods = new Dictionary<MethodReference, IMethod>();
            _localReferenceType = new ObjectReference(this);
        }

        public void AddField(FieldReference reference, IField field)
        {
            _fields[reference] = field;
        }

        public void AddMethod(MethodReference reference, IMethod method)
        {
            _methods[reference] = method;
        }

        public IType GetLocalReferenceType()
        {
            return _localReferenceType;
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetStruct(MangledName, type =>
            {
                IList<LLVMTypeRef> types = new List<LLVMTypeRef>();
                // Add a real body
                types.Add(LLVM.Int32TypeInContext(codeGenerator.Context));

                LLVM.StructSetBody(type, types.ToArray(), false);
            });
        }

        public LLVMValueRef GenerateConstantValue(CodeGenerator codeGenerator, object value)
        {
            throw new NotSupportedException("Constant classes not supported");
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

        public IType GetMostComplexType(IType other)
        {
            throw new NotImplementedException();
        }

        public IField ResolveField(FieldReference field)
        {
            return _fields[field];
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
            return _methods[method];
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            foreach (IField field in _fields.Values)
            {
                Console.WriteLine($" - Generating {field.Name}");
                field.Generate(assemblyLoader, codeGenerator);
            }

            foreach (IMethod method in _methods.Values)
            {
                Console.WriteLine($" - Generating {method.Name}");
                method.Generate(assemblyLoader, codeGenerator);
            }
        }
    }
}