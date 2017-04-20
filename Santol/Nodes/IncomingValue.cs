using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class IncomingValue : Node
    {
        public int Slot { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType { get; }

        public IncomingValue(Compiler compiler, TypeReference type, int slot) : base(compiler)
        {
            ResultType = type;
            Slot = slot;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(fgen.CurrentPhis[Slot]);
        }

        public override string ToFullString() => $"IncomingValue [Slot: {Slot}, Result: {ResultType}]";
    }
}