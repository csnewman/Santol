using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;
using Santol.Generator;

namespace Santol.Nodes
{
    public class Branch : Node
    {
        public CodeSegment Segment { get; }
        public NodeReference[] Values { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public Branch(Compiler compiler, CodeSegment segment, NodeReference[] values) : base(compiler)
        {
            Segment = segment;
            Values = values;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            LLVMValueRef[] vals = new LLVMValueRef[Values.Length];
            if (Segment.HasIncoming)
            {
                TypeReference[] targetTypes = Segment.Incoming;
                for (int i = 0; i < Values.Length; i++)
                    vals[Values.Length - 1 - i] = Values[i].GetLlvmRef(targetTypes[i]);
            }

            fgen.Branch(Segment, vals);
        }

        public override string ToFullString() => $"Branch [Target: {Segment.Name}]";
    }
}