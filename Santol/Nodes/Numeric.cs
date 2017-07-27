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
            DivideUnsigned,
            Remainder,
            RemainderUnsigned,
            ShiftLeft,
            ShiftRight,
            ShiftRightUnsigned,
            Not,
            And,
            Or,
            XOr,
            Negate
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
            if (lhs is EnumType)
                lhs = ((EnumType) lhs).UnderlyingType;
            LLVMValueRef lhsValue = Lhs.GetRef(codeGenerator, lhs);

            IType target = lhs;
            if (Operation == OperationType.Not)
            {
                SetRef(codeGenerator, target, LLVM.BuildNot(codeGenerator.Builder, lhsValue, ""));
                return;
            }
            else if (Operation == OperationType.Negate)
            {
                SetRef(codeGenerator, target, LLVM.BuildNeg(codeGenerator.Builder, lhsValue, ""));
                return;
            }

            IType rhs = Rhs.ResultType;

            if (rhs is EnumType)
                rhs = ((EnumType) rhs).UnderlyingType;
            LLVMValueRef rhsValue = Rhs.GetRef(codeGenerator, target);

            if (!lhs.IsStackCompatible(rhs))
                throw new NotImplementedException("Numeric ops on different types not implemented yet");

            switch (Operation)
            {
                case OperationType.Add:
                    SetRef(codeGenerator, target, LLVM.BuildAdd(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.Subtract:
                    SetRef(codeGenerator, target, LLVM.BuildSub(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.Multiply:
                    SetRef(codeGenerator, target, LLVM.BuildMul(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.Divide:
                    SetRef(codeGenerator, target, LLVM.BuildSDiv(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.DivideUnsigned:
                    SetRef(codeGenerator, target, LLVM.BuildUDiv(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.Remainder:
                    SetRef(codeGenerator, target, LLVM.BuildSRem(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.RemainderUnsigned:
                    SetRef(codeGenerator, target, LLVM.BuildURem(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.ShiftLeft:
                    SetRef(codeGenerator, target, LLVM.BuildShl(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.ShiftRight:
                    SetRef(codeGenerator, target, LLVM.BuildAShr(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.ShiftRightUnsigned:
                    SetRef(codeGenerator, target, LLVM.BuildLShr(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.And:
                    SetRef(codeGenerator, target, LLVM.BuildAnd(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.Or:
                    SetRef(codeGenerator, target, LLVM.BuildOr(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                case OperationType.XOr:
                    SetRef(codeGenerator, target, LLVM.BuildXor(codeGenerator.Builder, lhsValue, rhsValue, ""));
                    break;
                default:
                    throw new NotImplementedException("Unknown operationType " + Operation);
            }
        }

        public override string ToFullString()
            => $"Numeric [OperationType: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}