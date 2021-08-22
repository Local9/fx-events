namespace Lusive.Events.Generator.Serialization.Expressions
{
    public class ConstantExpression : IValueExpression
    {
        public static readonly ConstantExpression DefaultKeyword = new("default");
        
        public object Value { get; }

        public ConstantExpression(object value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}