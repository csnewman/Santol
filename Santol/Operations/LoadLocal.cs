using Mono.Cecil;
using Santol.Generator;

namespace Santol.Operations
{
    public class LoadLocal : IOperation
    {
        public Mono.Cecil.Cil.VariableDefinition Variable { get; }
        public TypeReference ResultType => Variable.VariableType;

        public LoadLocal(Mono.Cecil.Cil.VariableDefinition definition)
        {
            Variable = definition;
        }

        public void Generate(CodeGenerator cgen, FunctionGenerator fgen, StackBuilder stack)
        {
            stack.Push(fgen.LoadLocal(Variable.Index));
        }

        public string ToFullString() => $"LoadLocal [Variable: {Variable}, Type: {ResultType}]";
    }
}