using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class IncomingValue : Node
    {
        public int Slot { get; }
        public override bool HasResult => true;
        public override IType ResultType { get; }

        public IncomingValue(IType type, int slot)
        {
            ResultType = type;
            Slot = slot;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.CurrentPhis[Slot]);
        }

        public override string ToFullString() => $"IncomingValue [Slot: {Slot}, Result: {ResultType}]";
    }
}