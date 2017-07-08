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
//            LLVMValueRef[] args = new LLVMValueRef[Arguments.Length];
//            for (int i = 0; i < args.Length; i++)
//                args[args.Length - 1 - i] = Arguments[args.Length - 1 - i].GetRef(cgen,
//                    Method.Parameters[args.Length - 1 - i].ParameterType);
//
//            LLVMValueRef? val = fgen.GenerateCall(Method, args);
//            if (val.HasValue)
//                SetLlvmRef(val.Value);
            throw new NotImplementedException();
        }

        public override string ToFullString()
            =>
                $"CallVirtual [Instance: {Instance}, Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Method}, Result: {ResultType}]";
    }
}