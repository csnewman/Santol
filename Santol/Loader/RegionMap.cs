using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Santol.Loader
{
    public class RegionMap
    {
        public enum RegionType
        {
            Root,
            Try,
            Catch,
            Finally
        }

        public RegionType Type { get; }
        public InstructionRange Range { get; }
        public IList<RegionMap> AssociatedRegions { get; }
        public IList<RegionMap> ChildRegions { get; }
        public TypeReference CatchReference { get; internal set; }

        public RegionMap(RegionType type, InstructionRange range)
        {
            Type = type;
            Range = range;
            AssociatedRegions = new List<RegionMap>();
            ChildRegions = new List<RegionMap>();
        }

        public void AddAssociatedRegion(RegionMap regionMap)
        {
            AssociatedRegions.Add(regionMap);
        }

        public RegionMap AddRegion(RegionType type, InstructionRange range)
        {
            if (Range.Equals(range))
            {
                if (Type != type)
                    throw new NotSupportedException("Multipurpose regions!");
                return this;
            }
            if (!Range.IsContained(range))
                throw new ArgumentException("Cannot create sub regionMap with a subregion not contained");
            foreach (RegionMap sregion in ChildRegions)
                if (sregion.Range.IsContained(range))
                    return sregion.AddRegion(type, range);

            RegionMap newRegionMap = new RegionMap(type, range);
            ChildRegions.Add(newRegionMap);
            return newRegionMap;
        }

        public RegionMap GetRegion(Instruction instruction)
        {
            if (!Range.IsContained(instruction))
                throw new ArgumentOutOfRangeException();
            foreach (RegionMap childRegion in ChildRegions)
            {
                if (childRegion.Range.IsContained(instruction))
                    return childRegion.GetRegion(instruction);
            }
            return this;
        }

        public void EnsureEdges(IList<Instruction> locations)
        {
            if (!locations.Contains(Range.Start) ||
                (Range.End.Next != null && !locations.Contains(Range.End.Next)))
                throw new NotSupportedException("Unable to handle regionMap not segment bound");
            foreach (RegionMap childRegion in ChildRegions)
                childRegion.EnsureEdges(locations);
        }

        public void PrintTree(string ident)
        {
            Console.WriteLine($"{ident}{Type} ({Range.Start.Offset}-{Range.End.Offset})");
            foreach (RegionMap childRegion in ChildRegions)
                childRegion.PrintTree($"{ident}--- ");
        }
    }
}