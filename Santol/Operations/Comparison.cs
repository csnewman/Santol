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
            Equal,
            LessThan,
            GreaterThanOrEqual
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
            TypeReference target = TypeHelper.GetMostComplexType(Lhs, Rhs);

            LLVMValueRef v2 = stack.PopConverted(Rhs, target);
            LLVMValueRef v1 = stack.PopConverted(Lhs, target);


            switch (Operation)
            {
                case Operations.LessThan:
                    stack.PushConverted(fgen.CompareInts(LLVMIntPredicate.LLVMIntSLT, v1, v2), cgen.TypeSystem.Boolean,
                        ResultType);
                    break;
                case Operations.GreaterThanOrEqual:
                    stack.PushConverted(fgen.CompareInts(LLVMIntPredicate.LLVMIntSGE, v1, v2), cgen.TypeSystem.Boolean,
                        ResultType);
                    break;
                case Operations.Equal:
                    stack.PushConverted(fgen.CompareInts(LLVMIntPredicate.LLVMIntEQ, v1, v2), cgen.TypeSystem.Boolean,
                        ResultType);
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }

        public string ToFullString()
            => $"Comparison [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}