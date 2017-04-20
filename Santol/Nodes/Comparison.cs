using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

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
        public override TypeReference ResultType { get; }
        public OperationType Operation { get; }

        public Comparison(Compiler compiler, OperationType operationType, NodeReference lhs, NodeReference rhs)
            : base(compiler)
        {
            Operation = operationType;
            Lhs = lhs;
            Rhs = rhs;
            //TODO: Check whether change from int32 will break CIL code
            ResultType = compiler.TypeSystem.Boolean;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            TypeReference target = TypeHelper.GetMostComplexType(Lhs.ResultType, Rhs.ResultType);
            LLVMValueRef lhsValue = Lhs.GetLlvmRef(target);
            LLVMValueRef rhsValue = Rhs.GetLlvmRef(target);

            switch (Operation)
            {
                case OperationType.LessThan:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSLT, lhsValue, rhsValue));
                    break;
                case OperationType.GreaterThan:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSGT, lhsValue, rhsValue));
                    break;
                case OperationType.GreaterThanOrEqual:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSGE, lhsValue, rhsValue));
                    break;
                case OperationType.Equal:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
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