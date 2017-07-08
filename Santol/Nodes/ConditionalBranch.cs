using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;
using Santol.Generator;
using Santol.IR;
using Santol.Nodes;

namespace Santol.Nodes
{
    public class ConditionalBranch : Node
    {
        public Block Target { get; }
        public Block ElseTarget { get; }
        public NodeReference Condition { get; }
        public NodeReference[] Values { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public ConditionalBranch(Block target, Block elseTarget, NodeReference condition, NodeReference[] values)
        {
            Target = target;
            ElseTarget = elseTarget;
            Condition = condition;
            Values = values;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            LLVMValueRef condValueRef = Condition.GetRef(codeGenerator, PrimitiveType.Boolean);

            // TODO: Validate blocks have the same incomings

            LLVMValueRef[] vals = new LLVMValueRef[Values.Length];
            if (Target.HasIncoming)
            {
                IType[] targetTypes = Target.IncomingTypes;
                for (int i = 0; i < Values.Length; i++)
                    vals[Values.Length - 1 - i] = Values[i].GetRef(codeGenerator, targetTypes[i]);
            }
            fgen.BranchConditional(condValueRef, Target, ElseTarget, vals);
        }

        public override string ToFullString()
            => $"ConditionalBranch [Condition: {Condition}, Target: {Target.Name}, Else {ElseTarget.Name}]";
    }
}