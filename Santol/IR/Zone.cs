using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santol.IR
{
    public class Zone
    {
        public BlockRegion ParentRegion { get; }
        public BlockRegion TryRegion { get; internal set; }
        public IList<CatchCase> CatchRegions { get; }
        public BlockRegion FinalRegion { get; internal set; }

        public Zone(BlockRegion parentRegion)
        {
            ParentRegion = parentRegion;
        }

        internal void AddCatchRegion(CatchCase catchCase)
        {
            CatchRegions.Add(catchCase);
        }
    }
}