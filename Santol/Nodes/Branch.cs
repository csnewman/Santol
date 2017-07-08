using System;
using LLVMSharp;
using Mono.Cecil;
using Santol.Loader;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class Branch : Node
    {
        public Block Block { get; }
        public NodeReference[] Values { get; }
        public override bool HasResult => false;
        public override IType ResultType => null;

        public Branch(Block block, NodeReference[] values)
        {
            Block = block;
            Values = values;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            LLVMValueRef[] vals = new LLVMValueRef[Values.Length];
            if (Block.HasIncoming)
            {
                IType[] targetTypes = Block.IncomingTypes;
                for (int i = 0; i < Values.Length; i++)
                    vals[Values.Length - 1 - i] = Values[i].GetRef(codeGenerator, targetTypes[i]);
            }

            fgen.Branch(Block, vals);
        }

        public override string ToFullString() => $"Branch [Target: {Block.Name}]";
    }
}