using LLVMSharp;

namespace Santol.IR
{
    public interface IMethod
    {
        IType Parent { get; }
        string Name { get; }
        string MangledName { get; }
        bool IsStatic { get; }
        bool IsLocal { get; }
        bool ImplicitThis { get; }
        IType ReturnType { get; }
        IType[] Arguments { get; }

        LLVMValueRef? GenerateCall(LLVMValueRef[] arguments);
    }
}