using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Generator;

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
        public override TypeReference ResultType => Lhs.ResultType;
        public OperationType Operation { get; }

        public Numeric(Compiler compiler, OperationType operation, NodeReference lhs, NodeReference rhs) : base(compiler)
        {
            Operation = operation;
            Lhs = lhs;
            Rhs = rhs;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            TypeReference lhs = Lhs.ResultType;
            TypeReference rhs = Rhs.ResultType;

            if (CodeGenerator.IsEnum(lhs))
                lhs = CodeGenerator.GetEnumType(lhs);
            if (CodeGenerator.IsEnum(rhs))
                rhs = CodeGenerator.GetEnumType(rhs);

            if (lhs != rhs)
                throw new NotImplementedException("Numeric ops on different types not implemented yet");
            TypeReference target = lhs;

            LLVMValueRef lhsValue = Lhs.GetLlvmRef(target);
            LLVMValueRef rhsValue = Rhs.GetLlvmRef(target);

            switch (Operation)
            {
                case OperationType.Add:
                    SetLlvmRef(target, fgen.AddInts(lhsValue, rhsValue));
                    break;
                case OperationType.Subtract:
                    SetLlvmRef(target, fgen.SubtractInts(lhsValue, rhsValue));
                    break;
                case OperationType.Multiply:
                    SetLlvmRef(target, fgen.MultiplyInts(lhsValue, rhsValue));
                    break;
                case OperationType.Divide:
                    SetLlvmRef(target, fgen.DivideInts(lhsValue, rhsValue));
                    break;
                case OperationType.Remainder:
                    SetLlvmRef(target, fgen.RemainderInts(lhsValue, rhsValue));
                    break;
                case OperationType.ShiftLeft:
                    SetLlvmRef(target, fgen.ShiftLeft(lhsValue, rhsValue));
                    break;
                case OperationType.Or:
                    SetLlvmRef(target, fgen.Or(lhsValue, rhsValue));
                    break;
                case OperationType.XOr:
                    SetLlvmRef(target, fgen.XOr(lhsValue, rhsValue));
                    break;
                default:
                    throw new NotImplementedException("Unknown operationType " + Operation);
            }
        }

        public override string ToFullString()
            => $"Numeric [OperationType: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}