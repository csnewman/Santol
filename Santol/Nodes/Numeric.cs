using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Numeric : Node
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

        public NodeReference Lhs { get; }
        public NodeReference Rhs { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType => Lhs.ResultType;
        public Operations Operation { get; }

        public Numeric(Operations operation, NodeReference lhs, NodeReference rhs)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            TypeReference lhs = Lhs.ResultType;
            TypeReference rhs = Rhs.ResultType;

            if (cgen.IsEnum(lhs))
                lhs = cgen.GetEnumType(lhs);
            if (cgen.IsEnum(rhs))
                rhs = cgen.GetEnumType(rhs);

            if (lhs != rhs)
                throw new NotImplementedException("Numeric ops on different types not implemented yet");
            TypeReference target = lhs;

            LLVMValueRef lhsValue = Lhs.GetLlvmRef(cgen, target);
            LLVMValueRef rhsValue = Rhs.GetLlvmRef(cgen, target);

            switch (Operation)
            {
                case Operations.Add:
                    SetLlvmRef(cgen, target, fgen.AddInts(lhsValue, rhsValue));
                    break;
                case Operations.Subtract:
                    SetLlvmRef(cgen, target, fgen.SubtractInts(lhsValue, rhsValue));
                    break;
                case Operations.Multiply:
                    SetLlvmRef(cgen, target, fgen.MultiplyInts(lhsValue, rhsValue));
                    break;
                case Operations.Divide:
                    SetLlvmRef(cgen, target, fgen.DivideInts(lhsValue, rhsValue));
                    break;
                case Operations.Remainder:
                    SetLlvmRef(cgen, target, fgen.RemainderInts(lhsValue, rhsValue));
                    break;
                case Operations.ShiftLeft:
                    SetLlvmRef(cgen, target, fgen.ShiftLeft(lhsValue, rhsValue));
                    break;
                case Operations.Or:
                    SetLlvmRef(cgen, target, fgen.Or(lhsValue, rhsValue));
                    break;
                case Operations.XOr:
                    SetLlvmRef(cgen, target, fgen.XOr(lhsValue, rhsValue));
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }

        public override string ToFullString()
            => $"Numeric [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}