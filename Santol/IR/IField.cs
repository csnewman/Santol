using Santol.Generator;

namespace Santol.IR
{
    public interface IField
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        IType Type { get; }
        bool IsShared { get; }

        void Generate(CodeGenerator codeGenerator);
    }
}