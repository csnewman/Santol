using System.CodeDom;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class Return : IOperation
    {
        public bool HasValue => ValueType != null;
        public TypeReference ValueType { get; }
        public TypeReference ResultType => null;

        public Return(TypeReference valueType)
        {
            ValueType = valueType;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            fgen.Return(HasValue ? (LLVMValueRef?) stack.PopConverted(ValueType, fgen.Definition.ReturnType) : null);
        }

        public string ToFullString() => $"Return [HasValue: {HasValue}, Value: {ValueType}]";
    }
}