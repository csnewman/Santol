using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class LoadStatic : IOperation
    {
        public FieldReference Field { get; }
        public TypeReference ResultType => Field.FieldType;

        public LoadStatic(FieldReference definition)
        {
            Field = definition;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            stack.Push(fgen.LoadDirect(cgen.GetGlobal(Field.GetName(), cgen.ConvertType(Field.FieldType))));
        }

        public string ToFullString() => $"LoadLocal [Field: {Field}, Type: {ResultType}]";
    }
}