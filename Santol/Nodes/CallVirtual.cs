using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class CallVirtual : Node
    {
        public IMethod Method { get; }
        public NodeReference Instance { get; }
        public NodeReference[] Arguments { get; }
        public override bool HasResult => Method.ReturnType != PrimitiveType.Void;
        public override IType ResultType => Method.ReturnType;

        public CallVirtual(IMethod method, NodeReference instance, NodeReference[] arguments)
        {
            Method = method;
            Instance = instance;
            Arguments = arguments;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            LLVMValueRef function;
            if (Method.IsVirtual)
            {
                LLVMValueRef typeInfoPtr = LLVM.BuildLoad(codeGenerator.Builder,
                    Instance.ResultType.GetTypeInfoField(codeGenerator, Instance.GetRef()),
                    "");
                function = Method.Parent.TypeInfo.GetMethod(codeGenerator, Method, typeInfoPtr);
            }
            else
                function = Method.GetPointer(codeGenerator);

            if (Method.Arguments.Length != Arguments.Length + 1)
                throw new ArgumentException("Incorrect number of arguments!");

            LLVMValueRef[] args = new LLVMValueRef[Method.Arguments.Length];
            args[0] = Instance.GetRef(codeGenerator, Method.Arguments[0]);

            for (int i = 0; i < Method.Arguments.Length - 1; i++)
                args[1 + i] = Arguments[i].GetRef(codeGenerator, Method.Arguments[1 + i]);

            if (Method.ReturnType != PrimitiveType.Void)
                SetRef(LLVM.BuildCall(codeGenerator.Builder, function, args.ToArray(), ""));
            else
                LLVM.BuildCall(codeGenerator.Builder, function, args.ToArray(), "");
        }

        public override string ToFullString()
            =>
                $"CallVirtual [Instance: {Instance}, Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Method}, Result: {ResultType}]";
    }
}