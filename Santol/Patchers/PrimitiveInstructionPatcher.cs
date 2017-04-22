using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Santol.Loader;

namespace Santol.Patchers
{
    public class PrimitiveInstructionPatcher : IInstructionPatcher
    {
        public void Patch(Compiler compiler, MethodInfo method)
        {
            if (method.Definition.GetName() == "System_Object____.ctor______System_Void")
            {
                method.Definition.Body.Instructions.Clear();
                method.Processor.Emit(OpCodes.Ret);
            }
        }
    }
}