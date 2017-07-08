using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class Comparison : Node
    {
        public enum OperationType
        {
            Equal,
            LessThan,
            GreaterThanOrEqual,
            GreaterThan
        }

        public NodeReference Lhs { get; }
        public NodeReference Rhs { get; }
        public override bool HasResult => true;
        public override IType ResultType { get; }
        public OperationType Operation { get; }

        public Comparison(OperationType operationType, NodeReference lhs, NodeReference rhs)
        {
            Operation = operationType;
            Lhs = lhs;
            Rhs = rhs;
            //TODO: Check whether change from int32 will break CIL code
            ResultType = PrimitiveType.Boolean;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            IType target = Lhs.ResultType.GetMostComplexType(Rhs.ResultType);
            LLVMValueRef lhsValue = Lhs.GetRef(codeGenerator, target);
            LLVMValueRef rhsValue = Rhs.GetRef(codeGenerator, target);

            switch (Operation)
            {
                case OperationType.LessThan:
                    SetRef(codeGenerator, PrimitiveType.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSLT, lhsValue, rhsValue));
                    break;
                case OperationType.GreaterThan:
                    SetRef(codeGenerator, PrimitiveType.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSGT, lhsValue, rhsValue));
                    break;
                case OperationType.GreaterThanOrEqual:
                    SetRef(codeGenerator, PrimitiveType.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSGE, lhsValue, rhsValue));
                    break;
                case OperationType.Equal:
                    SetRef(codeGenerator, PrimitiveType.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntEQ, lhsValue, rhsValue));
                    break;
                default:
                    throw new NotImplementedException("Unknown operationType " + Operation);
            }
        }

        public override string ToFullString()
            => $"Comparison [OperationType: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}