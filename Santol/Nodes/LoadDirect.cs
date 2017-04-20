using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadDirect : Node
    {
        public NodeReference Address { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType { get; }

        public LoadDirect(Compiler compiler, TypeReference type, NodeReference address) : base(compiler)
        {
            ResultType = type;
            Address = address;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(fgen.LoadDirect(Address.GetLlvmRef(Compiler.TypeSystem.UIntPtr)));
        }

        public override string ToFullString() => $"LoadDirect [Address: {Address}, Type: {ResultType}]";
    }
}