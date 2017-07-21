using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Santol.Generator;

namespace Santol.IR
{
    public class TypeInfo
    {
        public string MangledName { get; }
        public TypeInfo ParentInfo { get; }
        private IList<IMethod> _methods;

        public TypeInfo(string mangledName, TypeInfo parent)
        {
            MangledName = mangledName;
            ParentInfo = parent;
            _methods = new List<IMethod>();
        }

        private IMethod GetBaseVersion(IMethod target)
        {
            foreach (IMethod method in _methods)
                if (method.SignatureMatches(target))
                    return method;
            return !target.IsVirtual ? null : ParentInfo?.GetBaseVersion(target);
        }

        public void RegisterMethod(IMethod method)
        {
            if (method.IsStatic)
                throw new ArgumentException("Expected local method");
            if (GetBaseVersion(method) != null)
                return;
            _methods.Add(method);
        }

        public LLVMTypeRef GetType(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetStruct($"{MangledName}_typeinfo_format", type =>
            {
                IList<LLVMTypeRef> types = new List<LLVMTypeRef>();

                // TODO: Class Info Pointer
                types.Add(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0));
                // TODO: Interface Map Pointer
                types.Add(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0));
                // TODO: Static Info Pointer
                types.Add(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0));

                AddMethods(codeGenerator, types);

                LLVM.StructSetBody(type, types.ToArray(), false);
            });
        }

        private void AddMethods(CodeGenerator codeGenerator, IList<LLVMTypeRef> types)
        {
            ParentInfo?.AddMethods(codeGenerator, types);

            // TODO: This param correction
            foreach (IMethod method in _methods)
                types.Add(LLVM.PointerType(method.GetMethodType(codeGenerator), 0));
        }

        public LLVMValueRef GetPointer(CodeGenerator codeGenerator)
        {
            return codeGenerator.GetGlobal($"{MangledName}_typeinfo", GetType(codeGenerator));
        }

        private void FillMethods(CodeGenerator codeGenerator, IType type, IList<LLVMValueRef> values)
        {
            ParentInfo?.FillMethods(codeGenerator, type, values);

            // TODO: Resolve method implementations
            foreach (IMethod method in _methods)
                values.Add(LLVM.ConstBitCast(type.FindMethodImplementation(method).GetPointer(codeGenerator),
                    LLVM.PointerType(method.GetMethodType(codeGenerator), 0)));
        }

        public void Generate(CodeGenerator codeGenerator, IType type)
        {
            IList<LLVMValueRef> values = new List<LLVMValueRef>();
            // TODO: Add real pointers
            values.Add(LLVM.ConstNull(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0)));
            values.Add(LLVM.ConstNull(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0)));
            values.Add(LLVM.ConstNull(LLVM.PointerType(LLVM.Int8TypeInContext(codeGenerator.Context), 0)));

            FillMethods(codeGenerator, type, values);

            LLVMValueRef value = LLVM.ConstNamedStruct(GetType(codeGenerator), values.ToArray());
            LLVMValueRef inst = codeGenerator.GetGlobal($"{MangledName}_typeinfo", GetType(codeGenerator));
            LLVM.SetInitializer(inst, value);
            LLVM.SetGlobalConstant(inst, true);
            LLVM.SetLinkage(inst, LLVMLinkage.LLVMExternalLinkage);
        }

        private bool FindMethod(ref int index, IMethod target)
        {
            if (ParentInfo?.FindMethod(ref index, target) ?? false)
                return true;

            foreach (IMethod method in _methods)
            {
                if (method.SignatureMatches(target))
                    return true;
                index++;
            }

            return false;
        }

        public LLVMValueRef GetMethod(CodeGenerator codeGenerator, IMethod method, LLVMValueRef objectPtr)
        {
            int index = 3;
            if (!FindMethod(ref index, method))
                throw new ArgumentException("Method not found");

            LLVMValueRef pointer = LLVM.BuildLoad(codeGenerator.Builder,
                LLVM.BuildStructGEP(codeGenerator.Builder, objectPtr, (uint) index, ""), "");

            return LLVM.BuildBitCast(codeGenerator.Builder, pointer, LLVM.PointerType(method.GetMethodType(codeGenerator), 0), "");
        }
    }
}