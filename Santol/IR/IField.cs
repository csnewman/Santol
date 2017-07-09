using Santol.Generator;
using Santol.Loader;

namespace Santol.IR
{
    public interface IField
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        IType Type { get; }
        bool IsShared { get; }

        void Generate(AssemblyLoader assemblyLoader, CodeGenerator codeGenerator);
    }
}