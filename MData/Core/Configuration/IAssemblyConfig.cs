using System.Collections.Generic;
using System.Reflection;

namespace MData.Core.Configuration
{
    public interface IAssemblyConfig
    {
        IAssemblyConfig OnlyLookIn(IEnumerable<Assembly> assemblies);
        IAssemblyConfig AssemblyName(string name);
        IAssemblyConfig Recreate(bool b);
        IAssemblyConfig UsingNamespace(string @namespace);
        IBaseTypeConfig With();
    }
}