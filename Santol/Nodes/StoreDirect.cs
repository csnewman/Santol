using System;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class StoreDirect : Node
    {
        public TypeReference Type { get; }
        public NodeReference Value { get; }
        public NodeReference Address { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public StoreDirect(TypeReference type, NodeReference value, NodeReference address)
        {
            Type = type;
            Value = value;
            Address = address;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetLlvmRef(cgen, Type), Address.GetLlvmRef(cgen, cgen.TypeSystem.UIntPtr));
        }

        public override string ToFullString()
            => $"StoreDirect [Value: {Value}, Address: {Address}, Type: {Type}]";
    }
}