using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public TypeInfo TypeInfo { get; private set; }
        public IType Parent { get; set; }
        private IOrderedDictionary _fields;
        private IOrderedDictionary _methods;
        private IType _localReferenceType;

        public ClassType(string name)
        {
            Name = name;
            MangledName = $"C_{name.Replace('.', '_')}";
            _fields = new OrderedDictionary();
            _methods = new OrderedDictionary();
            _localReferenceType = new ObjectReference(this);
        }

        public void AddField(FieldReference reference, IField field)
        {
            _fields.Add(reference, field);
        }

        public void AddMethod(MethodReference reference, IMethod method)
        {
            _methods.Add(reference, method);
        }

        public void Init()
        {
            TypeInfo = new TypeInfo(MangledName, Parent.TypeInfo);

            foreach (DictionaryEntry entry in _methods)
            {
                IMethod method = (IMethod) entry.Value;
                if (!method.IsStatic)
                    TypeInfo.RegisterMethod(method);
            }
        }

        public IType GetLocalReferenceType()
        {
            return _localReferenceType;
        }

        public IType GetStackType()
        {
            return _localReferenceType;
        }

        public bool IsInHierarchy(IType type)
        {
            if (Parent == null)
                return false;
            return Parent.Equals(type) || Parent.IsInHierarchy(type);
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetStruct(MangledName, type =>
            {
                IList<LLVMTypeRef> types = new List<LLVMTypeRef>();
                types.Add(Parent.GetType(codeGenerator));

                foreach (DictionaryEntry entry in _fields)
                {
                    IField field = (IField) entry.Value;
                    if (!field.IsShared)
                        types.Add(field.Type.GetType(codeGenerator));
                }

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
            return (IField) _fields[field];
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField target)
        {
            if (target.IsShared)
                throw new ArgumentException("Shared fields address can't be resolved on an object pointer");

            int index = 0;
            foreach (DictionaryEntry entry in _fields)
            {
                IField field = (IField) entry.Value;
                if (field.IsShared) continue;
                if (field.Equals(target))
                    return LLVM.BuildStructGEP(codeGenerator.Builder, objectPtr, (uint) index + 1, "");
                index++;
            }

            if (Parent != null)
                return Parent.GetFieldAddress(codeGenerator,
                    LLVM.BuildStructGEP(codeGenerator.Builder, objectPtr, 0, ""), target);
            throw new ArgumentException("Field not found");
        }

        public LLVMValueRef ExtractField(CodeGenerator codeGenerator, LLVMValueRef objectRef, IField field)
        {
            throw new NotImplementedException();
        }

        public IMethod ResolveMethod(AssemblyLoader assemblyLoader, MethodReference method)
        {
            return (IMethod) _methods[method];
        }

        public IMethod FindMethodImplementation(IMethod target)
        {
            foreach (DictionaryEntry entry in _methods)
            {
                IMethod method = (IMethod) entry.Value;
                if (method.SignatureMatches(target))
                    return method;
            }
            return Parent?.FindMethodImplementation(target);
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            TypeInfo.Generate(codeGenerator, this);

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