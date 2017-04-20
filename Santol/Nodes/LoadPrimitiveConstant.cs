using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadPrimitiveConstant : Node
    {
        public override bool HasResult => true;
        public override TypeReference ResultType { get; }

        public object Value { get; }

        public LoadPrimitiveConstant(Compiler compiler, TypeReference type, object value) : base(compiler)
        {
            ResultType = type;
            Value = value;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(CodeGenerator.GeneratePrimitiveConstant(ResultType, Value));
        }

        public override string ToFullString() => $"LoadPrimitiveConstant [Type: {ResultType}, Value: {Value}]";
    }
}