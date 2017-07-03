namespace Santol.IR
{
    public interface IMethod
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        IType ReturnType { get; }
        IType[] Arguments { get; }

    }
}