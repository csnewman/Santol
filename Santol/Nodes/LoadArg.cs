using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class LoadArg : Node
    {
        public override bool HasResult => true;
        public override IType ResultType { get; }
        public int Index { get; }

        public LoadArg(IType type, int index)
        {
            ResultType = type;
            Index = index;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.GetArgument(Index));
        }

        public override string ToFullString() => $"LoadArg [Index: {Index}, Type: {ResultType}]";
    }
}