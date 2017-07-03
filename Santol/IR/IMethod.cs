namespace Santol.IR
{
    public interface IMethod
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        bool IsStatic { get; }
        bool IsLocal { get; }
        IType ReturnType { get; }
        IType[] Arguments { get; }
    }
}