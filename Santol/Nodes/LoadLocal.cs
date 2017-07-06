using Mono.Cecil;
using Mono.Cecil.Cil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class LoadLocal : Node
    {
        public override bool HasResult => true;
        public override IType ResultType { get; }
        public int Index { get; }

        public LoadLocal(IType type, int index)
        {
            ResultType = type;
            Index = index;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.LoadLocal(Index));
        }

        public override string ToFullString() => $"LoadLocal [Index: {Index}, Type: {ResultType}]";
    }
}