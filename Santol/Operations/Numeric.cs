using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class Numeric : IOperation
    {
        public enum Operations
        {
            Add
        }

        public TypeReference Lhs { get; }
        public TypeReference Rhs { get; }
        public TypeReference ResultType { get; }
        public Operations Operation { get; }

        public Numeric(Operations operation, TypeReference lhs, TypeReference rhs, TypeReference type)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
            ResultType = type;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            LLVMValueRef v2 = stack.Pop();
            LLVMValueRef v1 = stack.Pop();

            if (Lhs != Rhs)
                throw new NotImplementedException("Numeric ops on different types not implemented yet");

            switch (Operation)
            {
                case Operations.Add:
                    stack.PushConverted(fgen.AddInts(v1, v2), Lhs, ResultType);
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }
        
        public string ToFullString()
            => $"Numeric [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}