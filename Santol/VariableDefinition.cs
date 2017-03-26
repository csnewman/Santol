using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Santol
{
    public class VariableDefinition
    {
        public int Index { get; }
        public string Name { get; }
        public TypeReference Type { get; }

        public VariableDefinition(int index, string name, TypeReference type)
        {
            Index = index;
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            //{(IsPinned ? " pinned" : "")}
            return $"{Index}: {Type.ToNiceString()} {Name}";
        }
    }
}