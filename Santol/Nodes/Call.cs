using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Call : Node
    {
        public MethodDefinition Definition;
        public NodeReference[] Arguments { get; }
        public override bool HasResult => Definition.ReturnType.MetadataType != MetadataType.Void;
        public override TypeReference ResultType => Definition.ReturnType;

        public Call(MethodDefinition definition, NodeReference[] arguments)
        {
            Definition = definition;
            Arguments = arguments;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            LLVMValueRef[] args = new LLVMValueRef[Arguments.Length];
            for (int i = 0; i < args.Length; i++)
                args[args.Length - 1 - i] = Arguments[args.Length - 1 - i].GetLlvmRef(cgen,
                    Definition.Parameters[args.Length - 1 - i].ParameterType);

            LLVMValueRef? val = fgen.GenerateCall(Definition, args);
            if (val.HasValue)
                SetLlvmRef(val.Value);
        }

        public override string ToFullString()
            =>
                $"Call [Args: [{string.Join<NodeReference>(", ", Arguments)}], Method: {Definition}, Result: {ResultType}]";
    }
}