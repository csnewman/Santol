using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class LoadArg : IOperation
    {
        public ParameterDefinition Parameter { get; }
        public int Slot => Parameter.Index;
        public TypeReference ResultType => Parameter.ParameterType;

        public LoadArg(ParameterDefinition definition)
        {
            Parameter = definition;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            stack.Push(fgen.GetParam(Slot));
        }

        public string ToFullString() => $"LoadArg [Slot: {Slot}, Parameter: {Parameter}, Type: {ResultType}]";
    }
}