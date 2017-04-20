using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Comparison : Node
    {
        public enum Operations
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
        public Operations Operation { get; }

        public Comparison(Compiler compiler, Operations operation, NodeReference lhs, NodeReference rhs)
            : base(compiler)
        {
            Operation = operation;
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
                case Operations.LessThan:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSLT, lhsValue, rhsValue));
                    break;
                case Operations.GreaterThan:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSGT, lhsValue, rhsValue));
                    break;
                case Operations.GreaterThanOrEqual:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntSGE, lhsValue, rhsValue));
                    break;
                case Operations.Equal:
                    SetLlvmRef(Compiler.TypeSystem.Boolean,
                        fgen.CompareInts(LLVMIntPredicate.LLVMIntEQ, lhsValue, rhsValue));
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }

        public override string ToFullString()
            => $"Comparison [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}