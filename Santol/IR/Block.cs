using System;
using System.Collections.Generic;
using Mono.Cecil;
using Santol.Nodes;

namespace Santol.IR
{
    public class Block
    {
        public string Name { get; }
        public BlockRegion Region { get; }
        public bool ForcedNoIncomings { get; internal set; }
        public IType[] IncomingTypes { get; }

        public Block(string name, BlockRegion region)
        {
            Name = name;
            Region = region;
        }
    }
}