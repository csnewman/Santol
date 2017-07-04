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

        public BlockRegion(RegionType type, Zone parent)
        {
            Type = type;
            ParentZone = parent;
            ChildZones = new List<Zone>();
        }

        public void AddChildZone(Zone zone)
        {
            ChildZones.Add(zone);
        }
    }
}