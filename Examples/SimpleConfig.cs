namespace RelaSharp.Examples
{
    class SimpleConfig
    {
        public readonly string Description;
        public readonly MemoryOrder MemoryOrder;

        public readonly bool ExpectedToFail;

        public SimpleConfig(string description, MemoryOrder memoryOrder, bool expectedToFail)
        {
            Description = description;
            MemoryOrder = memoryOrder;
            ExpectedToFail = expectedToFail;
        }
    }
}