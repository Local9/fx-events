namespace Lusive.Events.Generator.Serialization
{
    public enum InstructionType
    {
        None,
        Write,
        Read,
        Instantiate,
        Declare,
        Assign,
        Invoke,
        Extend,
        IterateIndex,
        IterateEnumerator,
        Block,
        NullCheck,
        Comment
    }
}