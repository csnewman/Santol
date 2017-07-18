using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class Numeric : Node
    {
        public enum OperationType
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

        public NodeReference Lhs { get; }
        public NodeReference Rhs { get; }
        public override bool HasResult => true;
        public override IType ResultType => Lhs.ResultType;
        public OperationType Operation { get; }

        public Numeric(OperationType operation, NodeReference lhs, NodeReference rhs)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            IType lhs = Lhs.ResultType;
            IType rhs = Rhs.ResultType;

            if (lhs is EnumType)
                lhs = ((EnumType) lhs).UnderlyingType;
            if (rhs is EnumType)
                rhs = ((EnumType) rhs).UnderlyingType;

            if (!lhs.IsStackCompatible(rhs))
                throw new NotImplementedException("Numeric ops on different types not implemented yet");
            IType target = lhs;

            LLVMValueRef lhsValue = Lhs.GetRef(codeGenerator, target);
            LLVMValueRef rhsValue = Rhs.GetRef(codeGenerator, target);

            switch (Operation)
            {
                case OperationType.Add:
                    SetRef(codeGenerator, target, fgen.AddInts(lhsValue, rhsValue));
                    break;
                case OperationType.Subtract:
                    SetRef(codeGenerator, target, fgen.SubtractInts(lhsValue, rhsValue));
                    break;
                case OperationType.Multiply:
                    SetRef(codeGenerator, target, fgen.MultiplyInts(lhsValue, rhsValue));
                    break;
                case OperationType.Divide:
                    SetRef(codeGenerator, target, fgen.DivideInts(lhsValue, rhsValue));
                    break;
                case OperationType.Remainder:
                    SetRef(codeGenerator, target, fgen.RemainderInts(lhsValue, rhsValue));
                    break;
                case OperationType.ShiftLeft:
                    SetRef(codeGenerator, target, fgen.ShiftLeft(lhsValue, rhsValue));
                    break;
                case OperationType.Or:
                    SetRef(codeGenerator, target, fgen.Or(lhsValue, rhsValue));
                    break;
                case OperationType.XOr:
                    SetRef(codeGenerator, target, fgen.XOr(lhsValue, rhsValue));
                    break;
                default:
                    throw new NotImplementedException("Unknown operationType " + Operation);
            }
        }

        public override string ToFullString()
            => $"Numeric [OperationType: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}