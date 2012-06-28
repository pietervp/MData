using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BLToolkit.Reflection;

namespace MData
{
    public static class sMDataExtensions
    {
        public static string GetName(this Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            return string.Format("{0}<{1}>", type.Name.Substring(0, type.Name.IndexOf('`')), type.GetGenericArguments().Select(x=> x.Name).Aggregate((x, y)=> x + ", " + y));
        }

        public static MethodInfo GetGenericMethod(this Type type, string name, Type[] genericTypeArgs, Type[] paramTypes, bool complain = true)
        {
            return 
                (
                from m in type.GetMethods()
                let pa = m.GetParameters() 
                where m.Name == name 
                where pa.Length == paramTypes.Length 
                select m.MakeGenericMethod(genericTypeArgs) 
                    into c 
                    where c.GetParameters().Select(p => p.ParameterType).SequenceEqual(paramTypes) 
                    select c
                ).FirstOrDefault();
        }

        public static IEnumerable<T> GetAttributes<T>(this Type target)
        {
            return TypeHelper.GetAttributes(target, typeof(T)).OfType<T>();
        }

        public static bool HasAttribute<T>(this Type target)
        {
            return target.GetAttributes<T>().Any();
        }

        public static string GetPropertyName<T, TU>(this Expression<Func<T, TU>> expression)
        {
            if (!(expression.Body is MemberExpression))
                return null;

            var memberExpression = expression.Body as MemberExpression;
            return memberExpression != null ? memberExpression.Member.Name : null;
        }
    }
}