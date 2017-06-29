using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class StoreField : Node
    {
        public NodeReference Object { get; }
        public FieldReference Field { get; }
        public override bool HasResult => false;
        public override TypeReference ResultType => null;

        public StoreField(Compiler compiler, NodeReference @object, FieldReference field)
            : base(compiler)
        {
            Object = @object;
            Field = field;
        }

        public override void Generate(FunctionGenerator fgen)
        {
//            fgen.StoreDirect(Value.GetLlvmRef(Type), Address.GetLlvmRef(Compiler.TypeSystem.UIntPtr));
            throw new NotImplementedException();
        }

        public override string ToFullString()
            => $"StoreField [Oject: {Object}, Field: {Field}]";
    }
}