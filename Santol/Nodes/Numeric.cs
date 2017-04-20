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

        public Numeric(Compiler compiler, Operations operation, NodeReference lhs, NodeReference rhs) : base(compiler)
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
                case Operations.Add:
                    SetLlvmRef(target, fgen.AddInts(lhsValue, rhsValue));
                    break;
                case Operations.Subtract:
                    SetLlvmRef(target, fgen.SubtractInts(lhsValue, rhsValue));
                    break;
                case Operations.Multiply:
                    SetLlvmRef(target, fgen.MultiplyInts(lhsValue, rhsValue));
                    break;
                case Operations.Divide:
                    SetLlvmRef(target, fgen.DivideInts(lhsValue, rhsValue));
                    break;
                case Operations.Remainder:
                    SetLlvmRef(target, fgen.RemainderInts(lhsValue, rhsValue));
                    break;
                case Operations.ShiftLeft:
                    SetLlvmRef(target, fgen.ShiftLeft(lhsValue, rhsValue));
                    break;
                case Operations.Or:
                    SetLlvmRef(target, fgen.Or(lhsValue, rhsValue));
                    break;
                case Operations.XOr:
                    SetLlvmRef(target, fgen.XOr(lhsValue, rhsValue));
                    break;
                default:
                    throw new NotImplementedException("Unknown operation " + Operation);
            }
        }

        public override string ToFullString()
            => $"Numeric [Operation: {Operation}, LHS: {Lhs}, RHS: {Rhs}, Result: {ResultType}]";
    }
}