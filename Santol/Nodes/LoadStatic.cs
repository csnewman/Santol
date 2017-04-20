using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Nodes
{
    public class LoadStatic : Node
    {
        public FieldReference Field { get; }
        public override bool HasResult => true;
        public override TypeReference ResultType => Field.FieldType;

        public LoadStatic(Compiler compiler, FieldReference definition) : base(compiler)
        {
            Field = definition;
        }

        public override void Generate(FunctionGenerator fgen)
        {
            SetLlvmRef(
                fgen.LoadDirect(CodeGenerator.GetGlobal(Field.GetName(), CodeGenerator.ConvertType(Field.FieldType))));
        }

        public override string ToFullString() => $"LoadLocal [Field: {Field}, Type: {ResultType}]";
    }
}