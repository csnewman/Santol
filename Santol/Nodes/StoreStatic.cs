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

        public StoreStatic(Compiler compiler, FieldReference destination, NodeReference value) : base(compiler)
        {
            Destination = destination;
            Value = value;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            fgen.StoreDirect(Value.GetLlvmRef(Destination.FieldType),
                CodeGenerator.GetGlobal(Destination.GetName(), CodeGenerator.ConvertType(Destination.FieldType)));
        }

        public override string ToFullString() => $"StoreStatic [Value: {Value}, Destination: {Destination}]";
    }
}