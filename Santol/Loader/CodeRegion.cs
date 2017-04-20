using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Santol.Loader
{
    public class CodeRegion
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
        public CodeRegion AssociatedRegion { get; }
        public IList<CodeRegion> ChildRegions { get; }

        public CodeRegion(RegionType type, InstructionRange range, CodeRegion associatedRegion)
        {
            Type = type;
            Range = range;
            AssociatedRegion = associatedRegion;
            ChildRegions = new List<CodeRegion>();
        }

        public CodeRegion AddRegion(RegionType type, InstructionRange range, CodeRegion associatedRegion)
        {
            if (Range.Equals(range))
            {
                if (Type != type)
                    throw new NotSupportedException("Multipurpose regions!");
                return this;
            }
            if (!Range.IsContained(range))
                throw new ArgumentException("Cannot create sub region with a subregion not contained");
            foreach (CodeRegion sregion in ChildRegions)
                if (sregion.Range.IsContained(range))
                    return sregion.AddRegion(type, range, associatedRegion);

            CodeRegion newRegion = new CodeRegion(type, range, associatedRegion);
            ChildRegions.Add(newRegion);
            return newRegion;
        }

        public CodeRegion GetRegion(Instruction instruction)
        {
            if (!Range.IsContained(instruction))
                throw new ArgumentOutOfRangeException();
            foreach (CodeRegion childRegion in ChildRegions)
            {
                if (childRegion.Range.IsContained(instruction))
                    return childRegion.GetRegion(instruction);
            }
            return this;
        }

        public void EnsureEdges(IList<Instruction> jumpDestinations)
        {
            if (!jumpDestinations.Contains(Range.Start) ||
                (Range.End.Next != null && !jumpDestinations.Contains(Range.End.Next)))
                throw new NotSupportedException("Unable to handle region not segment bound");
            foreach (CodeRegion childRegion in ChildRegions)
                childRegion.EnsureEdges(jumpDestinations);
        }

        public void PrintTree(string ident)
        {
            Console.WriteLine($"{ident}{Type} ({Range.Start.Offset}-{Range.End.Offset})");
            foreach (CodeRegion childRegion in ChildRegions)
                childRegion.PrintTree($"{ident}--- ");
        }
    }
}