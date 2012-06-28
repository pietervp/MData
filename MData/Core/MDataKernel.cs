using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninject;
using Ninject.Planning.Bindings;

namespace MData.Core
{
    public interface IResolver
    {
        T Resolve<T>() where T : class;
        Dictionary<Type, Type> GetInterfaceMapping();
        Type GetConcreteType(Type type);
    }

    public interface IAssemblyConfig
    {
        IAssemblyConfig OnlyLookIn(IEnumerable<Assembly> assemblies);
        IAssemblyConfig AssemblyName(string name);
        IAssemblyConfig Recreate(bool b);
        IAssemblyConfig UsingNamespace(string @namespace);
        IBaseTypeConfig With();
    }

    public interface IBaseTypeConfig
    {
        IBaseTypeConfig BaseTypeForEntity<TEntityBase>() where TEntityBase : EntityBase;
        IResolver GetResolver();
    }

    public class MDataConfigurator : IAssemblyConfig, IBaseTypeConfig
    {
        internal IEnumerable<Assembly> Assemblies;
        internal Type EntityType;
        internal Type BaseLogicType;
        internal string AssemblyNameForStorage;
        internal bool ShouldAlwaysRecreate;
        internal string Namespace;
        private static MDataKernel _mDataKernel;

        public MDataConfigurator()
        {
            AssemblyNameForStorage = @".\MData.Generated.Entities.dll";
            ShouldAlwaysRecreate = true;
            Namespace = "MData.Generated.Entities";
        }
        
        public static IAssemblyConfig Get()
        {
            return new MDataConfigurator();
        }

        IAssemblyConfig IAssemblyConfig.OnlyLookIn(IEnumerable<Assembly> assemblies)
        {
            Assemblies = assemblies;
            return this;
        }

        public IAssemblyConfig AssemblyName(string name)
        {
            AssemblyNameForStorage = name;
            return this;
        }

        public IAssemblyConfig Recreate(bool shouldRecreate)
        {
            ShouldAlwaysRecreate = shouldRecreate;
            return this;
        }

        public IAssemblyConfig UsingNamespace(string @namespace)
        {
            Namespace = @namespace;
            return null;
        }

        IBaseTypeConfig IAssemblyConfig.With()
        {
            return this;
        }

        IBaseTypeConfig IBaseTypeConfig.BaseTypeForEntity<TEntityBase>()
        {
            EntityType = typeof(TEntityBase);
            return this;
        }

        public static IResolver GetDefaultResolver()
        {
            return _mDataKernel;
        }

        public IResolver GetResolver()
        {
            _mDataKernel = new MDataKernel(this);
            return _mDataKernel;
        }
    }

    /// <summary>
    /// MDataKernel is the entry point of the framework.
    /// </summary>
    public class MDataKernel : IResolver
    {
        private MDataConfigurator Configurator { get; set; }
        private TypeCreator TypeHelper { get; set; }
        private StandardKernel Kernel { get; set; }

        private readonly Dictionary<Type, Type> _bindingCache;

        internal MDataKernel(MDataConfigurator configurator)
        {
            Configurator = configurator;
            Kernel = new StandardKernel();
            TypeHelper = new TypeCreator(Configurator);
            _bindingCache = new Dictionary<Type, Type>();
        }

        ~MDataKernel()
        {
            ExportAssembly();
        }

        public void ExportAssembly()
        {
            TypeHelper.SaveAssemblies();
        }

        /// <summary>
        /// Resolves an interface to a concrete type, by generating a proxy class that implements this interface.
        /// Resolve method will look for a LogicBase Implementor in the available assemblies, to execute custom code
        /// on the business object to generate.
        /// </summary>
        /// <typeparam name="T">Interface to resolve</typeparam>
        /// <param name="baseType">Basetype of the generated class</param>
        /// <returns>An instance of T</returns>
        public T Resolve<T>() where T : class
        {
            //check if this type is already registered in the Kernel
            IEnumerable<IBinding> resolve = Kernel.GetBindings(typeof (T));

            //if not found in kernel generate type and register it in the Kernel for future use
            if (resolve == null || !resolve.Any())
            {
                var domainInterface = TypeHelper.RegisterDomainInterface(typeof (T), TypeHelper.GetLogicClass(typeof (T)));

                if (domainInterface == null)
                    return null;

                Kernel.Bind<T>().To(domainInterface);
                _bindingCache.Add(typeof (T), domainInterface);
            }

            //return an instance of T
            return Kernel.Get<T>();
        }

        public Dictionary<Type, Type> GetInterfaceMapping()
        {
            return _bindingCache.ToDictionary(x => x.Key, x => x.Value);
        }

        public Type GetConcreteType(Type type)
        {
            //check if this type is already registered in the Kernel
            return !_bindingCache.ContainsKey(type) ? type : _bindingCache[type];
        }
    }
}