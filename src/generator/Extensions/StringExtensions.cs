namespace Lusive.Events.Generator.Extensions
{
    public static class StringExtensions
    {
        public static string Surround(this string value, string surrounding) =>
            Surround(value, surrounding, surrounding);

        public static string Surround(this string value, string before, string after) => $"{before}{value}{after}";

        public static string ToCamelCase(this string value)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > 1)
            {
                return char.ToLowerInvariant(value[0]) + value.Substring(1);
            }

            return value;
        }
    }
}