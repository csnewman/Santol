using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class StoreStatic : IOperation
    {
        public TypeReference SourceType { get; }
        public FieldReference Destination { get; }
        public TypeReference ResultType => null;

        public StoreStatic(FieldReference definition, TypeReference type)
        {
            SourceType = type;
            Destination = definition;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            fgen.StoreDirect(stack.PopConverted(SourceType, Destination.FieldType),
                cgen.GetGlobal(Destination.GetName(), cgen.ConvertType(Destination.FieldType)));
        }

        public string ToFullString() => $"StoreStatic [Source Type: {SourceType}, Destination: {Destination}]";
    }
}