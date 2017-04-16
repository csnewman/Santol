using System;
using Mono.Cecil;
using Santol.Generator;
using Santol.Nodes;

namespace Santol.Nodes
{
    public class StoreLocal : Node
    {
        public NodeReference Value { get; }
        public Mono.Cecil.Cil.VariableDefinition Destination { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public StoreLocal(Mono.Cecil.Cil.VariableDefinition destination, NodeReference value)
        {
            Destination = destination;
            Value = value;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            fgen.StoreLocal(Destination.Index, Value.GetLlvmRef(cgen, Destination.VariableType));
        }

        public override string ToFullString() => $"StoreLocal [Value: {Value}, Destination: {Destination}]";
    }
}