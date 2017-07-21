using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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


        private void AddFields(CodeGenerator codeGenerator, IList<LLVMTypeRef> types)
        {
            ClassType parent = Parent as ClassType;
            parent?.AddFields(codeGenerator, types);

            foreach (DictionaryEntry entry in _fields)
            {
                IField field = (IField) entry.Value;
                if (!field.IsShared)
                    types.Add(field.Type.GetType(codeGenerator));
            }
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetStruct(MangledName, type =>
            {
                IList<LLVMTypeRef> types = new List<LLVMTypeRef>();

                types.Add(LLVM.PointerType(TypeInfo.GetType(codeGenerator), 0));

                AddFields(codeGenerator, types);

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

        public LLVMValueRef Allocate(CodeGenerator codeGenerator)
        {
            LLVMTypeRef objectPtrType = GetStackType().GetType(codeGenerator);
            LLVMValueRef function =
                codeGenerator.GetFunction($"Allocate_{MangledName}",
                    LLVM.FunctionType(objectPtrType, new LLVMTypeRef[0], false));
            return LLVM.BuildCall(codeGenerator.Builder, function, new LLVMValueRef[0], "");
        }

        public LLVMValueRef GetTypeInfoField(CodeGenerator codeGenerator, LLVMValueRef objectPtr)
        {
            return LLVM.BuildStructGEP(codeGenerator.Builder, objectPtr, 0, "");
        }

        private bool FindField(ref int index, IField target)
        {
            ClassType parent = Parent as ClassType;
            if (parent?.FindField(ref index, target) ?? false)
                return true;

            foreach (DictionaryEntry entry in _fields)
            {
                IField field = (IField) entry.Value;
                if (field.IsShared) continue;
                if (field.Equals(target))
                    return true;
                index++;
            }

            return false;
        }

        public LLVMValueRef GetFieldAddress(CodeGenerator codeGenerator, LLVMValueRef objectPtr, IField target)
        {
            if (target.IsShared)
                throw new ArgumentException("Shared fields address can't be resolved on an object pointer");

            int index = 1;
            if (!FindField(ref index, target))
                throw new ArgumentException("Failed to find field");

            return LLVM.BuildStructGEP(codeGenerator.Builder, objectPtr, (uint) index, "");
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

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
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

            {
                LLVMTypeRef objectPtrType = GetStackType().GetType(codeGenerator);

                // Define function
                LLVMValueRef function =
                    codeGenerator.GetFunction($"Allocate_{MangledName}",
                        LLVM.FunctionType(objectPtrType, new LLVMTypeRef[0], false));
                LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
                FunctionGenerator functionGenerator = new FunctionGenerator(codeGenerator, null, function);
                functionGenerator.CreateBlock("entry", null);

                // Calculate size
                LLVMValueRef nullRef = LLVM.ConstNull(objectPtrType);
                LLVMValueRef sizeRef = LLVM.ConstGEP(nullRef,
                    new[] {LLVM.ConstInt(LLVM.Int32TypeInContext(codeGenerator.Context), 1, false)});
                LLVMValueRef objectSize = LLVM.BuildBitCast(codeGenerator.Builder, sizeRef,
                    PrimitiveType.UIntPtr.GetType(codeGenerator), "");

                // Allocate space
                LLVMValueRef spacePtr = Hooks.PlatformAllocate.GenerateCall(codeGenerator, new[] {objectSize}).Value;
                LLVMValueRef objectPtr = LLVM.BuildBitCast(codeGenerator.Builder, spacePtr, objectPtrType, "");

                // Return object pointer
                LLVM.BuildRet(codeGenerator.Builder, objectPtr);
            }
        }
    }
}