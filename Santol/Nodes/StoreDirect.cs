using System;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class StoreDirect : Node
    {
        public IType Type { get; }
        public NodeReference Value { get; }
        public NodeReference Address { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public StoreDirect(IType type, NodeReference value, NodeReference address)
        {
            Type = type;
            Value = value;
            Address = address;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetRef(codeGenerator, Type), Address.GetRef(codeGenerator, PrimitiveType.UIntPtr));
        }

        public override string ToFullString()
            => $"StoreDirect [Value: {Value}, Address: {Address}, Type: {Type}]";
    }
}