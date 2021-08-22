namespace Lusive.Events.Generator.Schemas
{
    public struct Schema
    {
        public readonly string Name;
        public SchemaMember[] Members;

        public Schema(string name, SchemaMember[] members)
        {
            Name = name;
            Members = members;
        }

        public Schema(string name)
        {
            Name = name;
            Members = null!;
        }
    }
}