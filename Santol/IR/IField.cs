namespace Santol.IR
{
    public interface IField
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        IType Type { get; }
    }
}