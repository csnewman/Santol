using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public interface IOperation
    {
        TypeReference ResultType { get; }
        void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack);
        string ToFullString();
    }
}