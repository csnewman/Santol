using System;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class Convert : Node
    {
        public NodeReference Value { get; }
        public override bool HasResult => true;
        public override IType ResultType { get; }

        public Convert(IType type, NodeReference value)
        {
            Value = value;
            ResultType = type;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(Value.GetRef(codeGenerator, ResultType));
        }

        public override string ToFullString() => $"Convert [Value: {Value}, Target: {ResultType}]";
    }
}