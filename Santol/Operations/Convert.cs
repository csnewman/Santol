using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class Convert : IOperation
    {
        public TypeReference SourceType { get; }
        public TypeReference ResultType { get; }

        public Convert(TypeReference source, TypeReference type)
        {
            SourceType = source;
            ResultType = type;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            stack.Push(cgen.GenerateConversion(SourceType, ResultType, stack.Pop()));
        }

        public string ToFullString() => $"Convert [Source: {SourceType}, Target: {ResultType}]";
    }
}