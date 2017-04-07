using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class LoadNullConstant : IOperation
    {
        public TypeReference ResultType => new TypeReference(null, null, null, null, false);

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            throw new System.NotImplementedException();
        }

        public string ToFullString() => "Null";
    }
}