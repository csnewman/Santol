using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadPrimitiveConstant : Node
    {
        public override bool HasResult => true;
        public override TypeReference ResultType { get; }

        public object Value { get; }

        public LoadPrimitiveConstant(TypeReference type, object value)
        {
            ResultType = type;
            Value = value;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            SetLlvmRef(cgen.GeneratePrimitiveConstant(ResultType, Value));
        }

        public override string ToFullString() => $"LoadPrimitiveConstant [Type: {ResultType}, Value: {Value}]";
    }
}