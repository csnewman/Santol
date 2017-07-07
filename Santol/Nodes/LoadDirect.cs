using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;
using Santol.IR;

namespace Santol.Nodes
{
    public class LoadDirect : Node
    {
        public NodeReference Address { get; }
        public override bool HasResult => true;
        public override IType ResultType { get; }

        public LoadDirect(IType type, NodeReference address)
        {
            ResultType = type;
            Address = address;
        }

        public override void Generate(CodeGenerator codeGenerator, FunctionGenerator fgen)
        {
            SetRef(fgen.LoadDirect(Address.GetRef(codeGenerator, PrimitiveType.UIntPtr)));
        }

        public override string ToFullString() => $"LoadDirect [Address: {Address}, Type: {ResultType}]";
    }
}