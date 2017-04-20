using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadArg : Node
    {
        public ParameterDefinition Parameter { get; }
        public int Slot => Parameter.Index;
        public override bool HasResult => true;
        public override TypeReference ResultType => Parameter.ParameterType;

        public LoadArg(Compiler compiler, ParameterDefinition definition) : base(compiler)
        {
            Parameter = definition;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(fgen.GetParam(Slot));
        }

        public override string ToFullString() => $"LoadArg [Slot: {Slot}, Parameter: {Parameter}, Type: {ResultType}]";
    }
}