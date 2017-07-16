using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public class FakeMethod : IMethod
    {
        public static readonly FakeMethod NoAction =
            new FakeMethod(PrimitiveType.Void, new IType[0], (generator, refs) => null);

        public IType Parent => throw new NotSupportedException();
        public string Name => throw new NotSupportedException();
        public string MangledName => throw new NotSupportedException();
        public bool IsStatic => throw new NotSupportedException();
        public bool IsLocal => throw new NotSupportedException();
        public int ArgumentOffset => throw new NotSupportedException();
        public IType ReturnType { get; }
        public IType[] Arguments { get; }
        public Func<CodeGenerator, LLVMValueRef[], LLVMValueRef?> Body { get; }

        public FakeMethod(IType returnType, IType[] arguments,
            Func<CodeGenerator, LLVMValueRef[], LLVMValueRef?> body)
        {
            ReturnType = returnType;
            Arguments = arguments;
            Body = body;
        }

        public void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator)
        {
            throw new NotSupportedException();
        }

        public LLVMTypeRef GetMethodType(CodeGenerator codeGenerator)
        {
            throw new NotSupportedException();
        }

        public LLVMValueRef? GenerateCall(CodeGenerator codeGenerator, LLVMValueRef[] arguments)
        {
            return Body(codeGenerator, arguments);
        }
    }
}