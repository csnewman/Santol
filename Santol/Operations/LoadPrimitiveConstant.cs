using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class LoadPrimitiveConstant : IOperation
    {
        public TypeReference ResultType { get; }

        public object Value { get; }

        public LoadPrimitiveConstant(TypeReference type, object value)
        {
            ResultType = type;
            Value = value;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            stack.Push(cgen.GeneratePrimitiveConstant(ResultType, Value));
        }

        public string ToFullString() => $"LoadPrimitiveConstant [Type: {ResultType}, Value: {Value}]";
    }
}