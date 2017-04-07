using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class StoreDirect : IOperation
    {
        public TypeReference ResultType => null;
        public TypeReference Type { get; }
        public TypeReference SourceType { get; }
        public TypeReference AddressType { get; }

        public StoreDirect(TypeReference type, TypeReference sourceType, TypeReference addressType)
        {
            Type = type;
            SourceType = sourceType;
            AddressType = addressType;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            fgen.StoreDirect(stack.PopConverted(SourceType, Type),
                stack.PopConverted(AddressType, cgen.TypeSystem.UIntPtr));
        }

        public string ToFullString()
            => $"StoreDirect [Source Type: {SourceType}, Address Type: {AddressType}, Type: {Type}]";
    }
}