using Santol.IR;

namespace Santol
{
    public static class Hooks
    {
        public static readonly IMethod PlatformAllocate = new ExternalMethod()
        {
            Name = "Santol.Platform.PlatformHooks.Allocate",
            MangledName = "C_Santol_Platform_PlatformHooks_SM_uintptr_Allocate_uintptr",
            IsStatic = true,
            Arguments = new IType[] {PrimitiveType.UIntPtr},
            ReturnType = PrimitiveType.UIntPtr
        };
    }
}