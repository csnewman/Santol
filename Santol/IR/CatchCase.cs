using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santol.IR
{
    public class CatchCase
    {
        public IType CatchType { get; }
        public BlockRegion Region { get; }

        public CatchCase(IType type, BlockRegion region)
        {
            CatchType = type;
            Region = region;
        }
    }
}