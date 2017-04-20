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

        public Convert(Compiler compiler, TypeReference type, NodeReference value) : base(compiler)
        {
            Value = value;
            ResultType = type;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(Value.GetLlvmRef(ResultType));
        }

        public override string ToFullString() => $"Convert [Value: {Value}, Target: {ResultType}]";
    }
}