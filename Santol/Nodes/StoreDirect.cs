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

        public StoreDirect(Compiler compiler, TypeReference type, NodeReference value, NodeReference address)
            : base(compiler)
        {
            Type = type;
            Value = value;
            Address = address;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetLlvmRef(Type), Address.GetLlvmRef(Compiler.TypeSystem.UIntPtr));
        }

        public override string ToFullString()
            => $"StoreDirect [Value: {Value}, Address: {Address}, Type: {Type}]";
    }
}