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
            Add,
            Subtract,
            Multiply,
            Divide,
            Remainder,
            ShiftLeft,
            Or,
            XOr
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

            TypeReference lhs = Lhs, rhs = Rhs;

            if (cgen.IsEnum(lhs))
                lhs = cgen.GetEnumType(lhs);
            if (cgen.IsEnum(rhs))
                rhs = cgen.GetEnumType(rhs);

            if (lhs != rhs)
                throw new NotImplementedException("Numeric ops on different types not implemented yet");

            switch (Operation)
            {
                case Operations.Add:
                    stack.PushConverted(fgen.AddInts(v1, v2), lhs, ResultType);
                    break;
                case Operations.Subtract:
                    stack.PushConverted(fgen.SubtractInts(v1, v2), lhs, ResultType);
                    break;
                case Operations.Multiply:
                    stack.PushConverted(fgen.MultiplyInts(v1, v2), lhs, ResultType);
                    break;
                case Operations.Divide:
                    stack.PushConverted(fgen.DivideInts(v1, v2), lhs, ResultType);
                    break;
                case Operations.Remainder:
                    stack.PushConverted(fgen.RemainderInts(v1, v2), lhs, ResultType);
                    break;
                case Operations.ShiftLeft:
                    stack.PushConverted(fgen.ShiftLeft(v1, v2), lhs, ResultType);
                    break;
                case Operations.Or:
                    stack.PushConverted(fgen.Or(v1, v2), lhs, ResultType);
                    break;
                case Operations.XOr:
                    stack.PushConverted(fgen.XOr(v1, v2), lhs, ResultType);
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }

        public string ToFullString()
            => $"Numeric [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}