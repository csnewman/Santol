using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class StoreLocal : IOperation
    {
        public TypeReference SourceType { get; }
        public Mono.Cecil.Cil.VariableDefinition Destination { get; }
        public TypeReference ResultType => null;

        public StoreLocal(Mono.Cecil.Cil.VariableDefinition definition, TypeReference type)
        {
            SourceType = type;
            Destination = definition;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            fgen.StoreLocal(Destination.Index, stack.PopConverted(SourceType, Destination.VariableType));
        }

        public string ToFullString() => $"StoreLocal [Source Type: {SourceType}, Destination: {Destination}]";
    }
}