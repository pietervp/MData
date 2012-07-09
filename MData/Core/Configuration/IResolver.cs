using System;
using System.Collections.Generic;

namespace MData.Core.Configuration
{
    public interface IResolver
    {
        T Resolve<T>() where T : class;
        object Resolve(Type type);

        Dictionary<Type, Type> GetPersistableInterfaceMapping();
        Type GetConcreteType(Type type);
        IResolver AutoDiscover();
    }
}