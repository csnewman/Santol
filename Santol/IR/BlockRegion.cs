using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santol.IR
{
    public class BlockRegion
    {
        public enum RegionType
        {
            Primary,
            Catch,
            Final
        }

        public RegionType Type { get; }
        public Zone ParentZone { get; }
        public IList<Zone> ChildZones { get; }
        public IList<Block> Blocks { get; }

        public BlockRegion(RegionType type, Zone parent)
        {
            Type = type;
            ParentZone = parent;
            ChildZones = new List<Zone>();
            Blocks = new List<Block>();
        }

        public void AddChildZone(Zone zone)
        {
            ChildZones.Add(zone);
        }

        public void AddBlock(Block block)
        {
            Blocks.Add(block);
        }
    }
}