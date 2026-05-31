using System.Linq.Expressions;
using System.Reflection;

namespace ProductManagementSystem.Common
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyName, string direction)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return source;

            PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null) return source; // Return unsorted if the property is not found

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            Expression propertyAccess = Expression.Property(parameter, property);
            LambdaExpression orderByExpression = Expression.Lambda(propertyAccess, parameter);

            string methodName = direction?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";

            MethodInfo orderByMethod = typeof(Queryable).GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(typeof(T), property.PropertyType);

            object result = orderByMethod.Invoke(null, new object[] { source, orderByExpression });

            return (IQueryable<T>)result!;
        }
    }
}
