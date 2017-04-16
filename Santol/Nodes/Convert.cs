using System;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Convert : Node
    {
        public NodeReference Value { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType { get; }

        public Convert(TypeReference type, NodeReference value)
        {
            Value = value;
            ResultType = type;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            SetLlvmRef(Value.GetLlvmRef(cgen, ResultType));
        }

        public override string ToFullString() => $"Convert [Value: {Value}, Target: {ResultType}]";
    }
}