using Lusive.Events.Generator.Generation;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization
{
    public abstract class BaseConverter
    {
        public abstract bool Criteria(DataType dataType, ITypeSymbol type, string qualified);

        public abstract BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context);

        public abstract BaseInstruction[] Read(Language language, GenerationHost host,
            TypeSerializationContext context);

        public BaseInstruction[] Combine(params BaseInstruction[][] arrays)
        {
            var length = 0;

            foreach (var array in arrays)
            {
                length += array.Length;
            }

            var result = new BaseInstruction[length];
            var position = 0;

            foreach (var array in arrays)
            {
                array.CopyTo(result, position);
                position += array.Length;
            }

            return result;
        }

        public BaseInstruction[] Combine(BaseInstruction[][] value, params BaseInstruction[][] args)
        {
            var first = Combine(value);
            var second = Combine(args);
            var length = first.Length + second.Length;
            var result = new BaseInstruction[length];

            first.CopyTo(result, 0);
            second.CopyTo(result, first.Length);

            return result;
        }
    }
}