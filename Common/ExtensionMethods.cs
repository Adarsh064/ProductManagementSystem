using static ProductManagementSystem.Common.Enums;

namespace ProductManagementSystem.Common
{
    public static class ExtensionMethods
    {
        public static string GetStringValue(this Enum value)
        {
            var stringValue = value.ToString();
            var type = value.GetType();
            var fieldInfo = type.GetField(value.ToString());
            var attrs = fieldInfo?.GetCustomAttributes(typeof(StringValue), false) as StringValue[];

            if (attrs?.Length > 0)
            {
                stringValue = attrs[0].Value;
            }
            return stringValue;
        }

        public static bool IsSimpleType(Type type) =>
            type.IsPrimitive ||
            new Type[] {
                typeof(Enum),
                typeof(String),
                typeof(Decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid)
            }.Contains(type) ||
            Convert.GetTypeCode(type) != TypeCode.Object ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
            IsSimpleType(type.GetGenericArguments()[0]));

        public static object? GetValByName(this object obj, string propertyName)
        {
            return obj?.GetType()?.GetProperty(propertyName)?.GetValue(obj, null);
        }
    }
}
