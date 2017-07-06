using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class LoadPrimitiveConstant : Node
    {
        public override bool HasResult => true;
        public override IType ResultType { get; }
        public object Value { get; }

        public LoadPrimitiveConstant(IType type, object value)
        {
            ResultType = type;
            Value = value;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(ResultType.GenerateConstantValue(codeGenerator, Value));
        }

        public override string ToFullString() => $"LoadPrimitiveConstant [Type: {ResultType}, Value: {Value}]";
    }
}