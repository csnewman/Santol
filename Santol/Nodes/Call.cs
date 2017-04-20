using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Call : Node
    {
        public MethodReference Method;
        public NodeReference[] Arguments { get; }
        public override bool HasResult => Method.ReturnType.MetadataType != MetadataType.Void;
        public override TypeReference ResultType => Method.ReturnType;

        public Call(Compiler compiler, MethodReference method, NodeReference[] arguments) : base(compiler)
        {
            Method = method;
            Arguments = arguments;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            LLVMValueRef[] args = new LLVMValueRef[Arguments.Length];
            for (int i = 0; i < args.Length; i++)
                args[args.Length - 1 - i] = Arguments[args.Length - 1 - i].GetLlvmRef(
                    Method.Parameters[args.Length - 1 - i].ParameterType);

            LLVMValueRef? val = fgen.GenerateCall(Method, args);
            if (val.HasValue)
                SetLlvmRef(val.Value);
        }

        public override string ToFullString()
            =>
                $"Call [Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Method}, Result: {ResultType}]";
    }
}