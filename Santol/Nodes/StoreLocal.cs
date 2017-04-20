using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Generator;
using Santol.Nodes;

namespace Santol.Nodes
{
    public class StoreLocal : Node
    {
        public NodeReference Value { get; }
        public VariableDefinition Destination { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public StoreLocal(Compiler compiler, VariableDefinition destination, NodeReference value) : base(compiler)
        {
            Destination = destination;
            Value = value;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            fgen.StoreLocal(Destination.Index, Value.GetLlvmRef(Destination.VariableType));
        }

        public override string ToFullString() => $"StoreLocal [Value: {Value}, Destination: {Destination}]";
    }
}