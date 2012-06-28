using System;
using System.Collections.Generic;

namespace MData.Core.Configuration
{
    public interface IResolver
    {
        T Resolve<T>() where T : class;
        Dictionary<Type, Type> GetInterfaceMapping();
        Type GetConcreteType(Type type);
    }
}