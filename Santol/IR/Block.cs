using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Santol.Nodes;

namespace Santol.IR
{
    public class Block
    {
        public string Name { get; }
        public BlockRegion Region { get; }
        public bool ForcedNoIncomings { get; internal set; }
        public IType[] IncomingTypes { get; private set; }
        public bool HasIncoming => !ForcedNoIncomings && IncomingTypes != null && IncomingTypes.Length > 0;
        public IList<Block> CallingBlocks { get; }

        public Block(string name, BlockRegion region)
        {
            Name = name;
            Region = region;
            CallingBlocks = new List<Block>();
        }
        
        public void AddCaller(Block block, IType[] incomingTypes)
        {
            CallingBlocks.Add(block);

            if (incomingTypes.Length != 0 && ForcedNoIncomings)
                throw new NotSupportedException("Caller provided stack data for block which has forced no incomings");
            if (IncomingTypes == null)
                IncomingTypes = incomingTypes;
            else if (incomingTypes.Length != IncomingTypes.Length)
                throw new NotSupportedException("Unable to handle a different number of incoming types into a block");
            else if (!incomingTypes.SequenceEqual(IncomingTypes))
                for (int i = 0; i < IncomingTypes.Length; i++)
                {
                    if (IncomingTypes[i].Equals(incomingTypes[i]))
                        continue;

                    Console.WriteLine(
                        $"WARNING: Having to get simplest type between {IncomingTypes[i]} and {incomingTypes[i]}, untested legacy functionality being used!");
                    IncomingTypes[i] = TypeHelper.GetSimplestType(IncomingTypes[i], incomingTypes[i]);
                }
        }
    }
}