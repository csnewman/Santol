using System;
using System.Collections.Generic;
using MMethodDefinition = Mono.Cecil.MethodDefinition;

namespace Santol
{
    public class MethodDefinition
    {
        public string Name => Definition.Name;
        public MMethodDefinition Definition { get; }
        public IList<VariableDefinition> Locals { get; }

        public MethodDefinition(MMethodDefinition definition)
        {
            Definition = definition;
            Locals = new List<VariableDefinition>();
        }

        public void AddLocal(VariableDefinition variable)
        {
            Locals.Add(variable);
        }
    }
}