using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class Comparison : IOperation
    {
        public enum Operations
        {
            LessThan
        }

        public TypeReference Lhs { get; }
        public TypeReference Rhs { get; }
        public TypeReference ResultType { get; }
        public Operations Operation { get; }

        public Comparison(TypeSystem typeSystem, Operations operation, TypeReference lhs, TypeReference rhs)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            //TODO: Check whether change from int32 will break CIL code
            ResultType = typeSystem.Boolean;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            LLVMValueRef v2 = stack.Pop();
            LLVMValueRef v1 = stack.Pop();

            if (Lhs != Rhs)
                throw new NotImplementedException("Comparison ops on different types not implemented yet");

            switch (Operation)
            {
                case Operations.LessThan:
                    stack.PushConverted(fgen.CompareInts(LLVMIntPredicate.LLVMIntSLT, v1, v2), cgen.TypeSystem.Boolean, ResultType);
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }
        
        public string ToFullString()
            => $"Comparison [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}