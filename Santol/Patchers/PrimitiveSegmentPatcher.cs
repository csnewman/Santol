using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Santol.Loader;
using Santol.Nodes;

namespace Santol.Patchers
{
    public class PrimitiveSegmentPatcher : ISegmentPatcher
    {
        public void Patch(Compiler compiler, MethodInfo method, CodeSegment segment)
        {
            foreach (NodeReference nodeRef in segment.Nodes)
            {
                Node oldNode = nodeRef.Node;
                if (oldNode is Call)
                {
                    Call callNode = (Call) oldNode;
                    string methodName = callNode.Method.GetName();

                    if (CallToConversionMap.ContainsKey(methodName))
                        oldNode.Replace(new Nodes.Convert(compiler, CallToConversionMap[methodName](compiler.TypeSystem),
                            callNode.Arguments[0]));
                }
            }
        }

        private delegate TypeReference TypeInst(TypeSystem ts);

        private static readonly IDictionary<string, TypeInst> CallToConversionMap;

        static PrimitiveSegmentPatcher()
        {
            CallToConversionMap = new Dictionary<string, TypeInst>
            {
                {"System_UIntPtr____op_Explicit___System_UInt64___System_UIntPtr", ts => ts.UIntPtr},
                {"System_UIntPtr____op_Explicit___System_UInt32___System_UIntPtr", ts => ts.UIntPtr}
            };
        }
    }
}