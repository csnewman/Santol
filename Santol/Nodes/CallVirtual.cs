using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class CallVirtual : Node
    {
        public MethodReference Method { get; }
        public NodeReference Instance { get; }
        public NodeReference[] Arguments { get; }
        public override bool HasResult => Method.ReturnType.MetadataType != MetadataType.Void;
        public override TypeReference ResultType => Method.ReturnType;

        public CallVirtual(Compiler compiler, MethodReference method, NodeReference instance, NodeReference[] arguments)
            : base(compiler)
        {
            Method = method;
            Instance = instance;
            Arguments = arguments;
        }

        public override void Generate(FunctionGenerator fgen)
        {
//            LLVMValueRef[] args = new LLVMValueRef[Arguments.Length];
//            for (int i = 0; i < args.Length; i++)
//                args[args.Length - 1 - i] = Arguments[args.Length - 1 - i].GetLlvmRef(cgen,
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