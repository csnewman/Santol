using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;
using Santol.Generator;

namespace Santol.Operations
{
    public class Branch : IOperation
    {
        public CodeSegment Segment { get; }
        public TypeReference[] Types { get; }
        public TypeReference ResultType => null;

        public Branch(CodeSegment segment, TypeReference[] types)
        {
            Segment = segment;
            Types = types;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            LLVMValueRef[] vals = new LLVMValueRef[Types.Length];
            if (Segment.HasIncoming)
            {
                TypeReference[] targetTypes = Segment.Incoming;
                for (int i = 0; i < Types.Length; i++)
                    vals[Types.Length - 1 - i] = stack.PopConverted(Types[i], targetTypes[i]);
            }

            fgen.Branch(Segment, vals);
        }

        public string ToFullString() => $"Branch [Target: {Segment.Name}]";
    }
}