using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MData.Core.Base
{
    public class Reflect<T>
    {
        public static MethodInfo GetMethod(Expression<Action<T>> membercall)
        {
            var methodCallExpression = membercall.Body as MethodCallExpression;
            if (methodCallExpression != null)
            {
                if(methodCallExpression.Method.IsGenericMethod)
                    return methodCallExpression.Method.GetGenericMethodDefinition();

                return methodCallExpression.Method;
            }

            return  null;
        }
    }
}