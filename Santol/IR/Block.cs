namespace Santol.IR
{
    public class Block
    {
        public string Name { get; }
        public BlockRegion Region { get; }

        public Block(string name, BlockRegion region)
        {
            Name = name;
            Region = region;
        }
    }
}