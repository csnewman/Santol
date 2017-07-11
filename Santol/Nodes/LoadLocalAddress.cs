using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class LoadLocalAddress : Node
    {
        public override bool HasResult => true;
        public override IType ResultType { get; }
        public int Index { get; }

        public LoadLocalAddress(IType type, int index)
        {
            ResultType = type;
            Index = index;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.Locals[Index]);
        }

        public override string ToFullString() => $"LoadLocalAddress [Index: {Index}, Type: {ResultType}]";
    }
}