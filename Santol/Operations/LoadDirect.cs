using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class LoadDirect : IOperation
    {
        public TypeReference AddressType { get; }
        public TypeReference ResultType { get; }

        public LoadDirect(TypeReference type, TypeReference address)
        {
            ResultType = type;
            AddressType = address;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            stack.Push(fgen.LoadDirect(stack.PopConverted(AddressType, cgen.TypeSystem.UIntPtr)));
        }

        public string ToFullString() => $"LoadDirect [Address: {AddressType}, Type: {ResultType}]";
    }
}