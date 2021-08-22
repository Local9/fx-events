using System;

namespace Lusive.Events.Generator.Serialization.Identifiers
{
    public static class IdentifierContext
    {
        public static int Defined { get; private set; }

        public static string CreateIdentifier(string name)
        {
            Defined++;
            
            return $"{name}{Convert.ToInt32(Defined)}";
        }

        public static void Reset()
        {
            Defined = 0;
        }
    }
}