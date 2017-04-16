using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;
using Santol.Generator;
using Santol.Nodes;

namespace Santol.Nodes
{
    public class ConditionalBranch : Node
    {
        public CodeSegment Segment { get; }
        public CodeSegment ElseSegment { get; }
        public NodeReference Condition { get; }
        public NodeReference[] Values { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public ConditionalBranch(CodeSegment segment, CodeSegment elseSegment, NodeReference condition,
            NodeReference[] values)
        {
            Segment = segment;
            ElseSegment = elseSegment;
            Condition = condition;
            Values = values;
        }

        public override void Generate(CodeGenerator cgen, FunctionGenerator fgen)
        {
            LLVMValueRef condValueRef = Condition.GetLlvmRef(cgen, cgen.TypeSystem.Boolean);

            LLVMValueRef[] vals = new LLVMValueRef[Values.Length];
            if (Segment.HasIncoming)
            {
                TypeReference[] targetTypes = Segment.Incoming;
                for (int i = 0; i < Values.Length; i++)
                    vals[Values.Length - 1 - i] = Values[i].GetLlvmRef(cgen, targetTypes[i]);
            }
            fgen.BranchConditional(condValueRef, Segment, ElseSegment, vals);
        }

        public override string ToFullString()
            => $"ConditionalBranch [Condition: {Condition}, Target: {Segment.Name}, Else {ElseSegment.Name}]";
    }
}