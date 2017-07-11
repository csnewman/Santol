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
    public class SequentialStructType : IType
    {
        public string Name { get; }
        public string MangledName { get; }
        public bool IsAllowedOnStack => true;
        public bool Packed { get; }
        private IOrderedDictionary _fields;

        public SequentialStructType(string name, bool packed)
        {
            Name = name;
            MangledName = $"SS_{name.Replace('.', '_')}";
            Packed = packed;
            _fields = new OrderedDictionary();
        }

        public void AddField(FieldReference reference, IField field)
        {
            _fields.Add(reference, field);
        }

        public IType GetLocalReferenceType()
        {
            throw new NotImplementedException();
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetStruct(MangledName, type =>
            {
                IList<LLVMTypeRef> types = new List<LLVMTypeRef>();

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
            throw new NotSupportedException("Constant sequential structs not supported");
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
            return (IField) _fields[field];
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField target)
        {
            if (target.IsShared)
                throw new ArgumentException();

            int index = 0;
            bool found = false;
            foreach (DictionaryEntry entry in _fields)
            {
                IField field = (IField) entry.Value;
                if (field.IsShared) continue;
                if (field.Equals(target))
                {
                    found = true;
                    break;
                }
                index++;
            }
            if (!found)
                throw new ArgumentException();

            return LLVM.BuildStructGEP(codeGenerator.Builder, objectPtr, (uint) index, "");
        }

        public IMethod ResolveMethod(MethodReference method)
        {
            throw new NotImplementedException();
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            foreach (IField field in _fields.Values)
                field.Generate(assemblyLoader, codeGenerator);
            //            throw new NotImplementedException();
        }
    }
}