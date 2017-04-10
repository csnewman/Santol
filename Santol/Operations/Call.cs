using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class Call : IOperation
    {
        public MethodDefinition Definition;
        public TypeReference[] ArgTypes { get; }
        public TypeReference ResultType => Definition.ReturnType;

        public Call(MethodDefinition definition, TypeReference[] argTypes)
        {
            Definition = definition;
            ArgTypes = argTypes;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            LLVMValueRef[] args = new LLVMValueRef[ArgTypes.Length];
            for (int i = 0; i < args.Length; i++)
                args[args.Length - 1 - i] = stack.PopConverted(ArgTypes[args.Length - 1 -  i], Definition.Parameters[args.Length - 1 -  i].ParameterType);
            
            LLVMValueRef? val = fgen.GenerateCall(Definition, ArgTypes, args);
            if (val.HasValue)
                stack.Push(val.Value);
        }

        public string ToFullString()
            =>
                $"Call [Args: [{string.Join<TypeReference>(", ", ArgTypes)}], Method: {Definition}, Result: {ResultType}]";
    }
}