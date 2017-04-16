using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class StoreStatic : Node
    {
        public NodeReference Value { get; }
        public FieldReference Destination { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public StoreStatic(FieldReference destination, NodeReference value)
        {
            Destination = destination;
            Value = value;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetLlvmRef(cgen, Destination.FieldType),
                cgen.GetGlobal(Destination.GetName(), cgen.ConvertType(Destination.FieldType)));
        }

        public override string ToFullString() => $"StoreStatic [Value: {Value}, Destination: {Destination}]";
    }
}