using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil.Cil;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class ExternalMethod : IMethod
    {
        public IType Parent { set; get; }
        public string Name { set; get; }
        public string MangledName { set; get; }
        public bool IsStatic { set; get; }
        public bool IsLocal { set; get; }
        public bool IsVirtual { get; set; }
        public int ArgumentOffset { set; get; }
        public IType ReturnType { set; get; }
        public IType[] Arguments { set; get; }


        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }

        public LLVMTypeRef GetMethodType(CodeGenerator codeGenerator)
        {
            LLVMTypeRef returnType = ReturnType.GetType(codeGenerator);
            LLVMTypeRef[] argTypes = new LLVMTypeRef[Arguments.Length];

            for (int i = 0; i < Arguments.Length; i++)
                argTypes[i] = Arguments[i].GetType(codeGenerator);

            return LLVM.FunctionType(returnType, argTypes, false);
        }

        public LLVMValueRef GetPointer(CodeGenerator codeGenerator)
        {
            throw new NotImplementedException();
        }

        public LLVMValueRef? GenerateCall(CodeGenerator codeGenerator, LLVMValueRef[] arguments)
        {
            LLVMTypeRef type = GetMethodType(codeGenerator);
            LLVMValueRef function = codeGenerator.GetFunction(MangledName, type);

            if (ReturnType != PrimitiveType.Void)
                return LLVM.BuildCall(codeGenerator.Builder, function, arguments, "");
            LLVM.BuildCall(codeGenerator.Builder, function, arguments, "");
            return null;
        }
    }
}