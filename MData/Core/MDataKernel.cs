using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MData.Attributes;
using MData.Core.Configuration;
using Ninject;
using Ninject.Planning.Bindings;

namespace MData.Core
{
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
            return Resolve(typeof(T)) as T;
        }

        /// <summary>
        /// Resolves an interface to a concrete type, by generating a proxy class that implements this interface.
        /// Resolve method will look for a LogicBase Implementor in the available assemblies, to execute custom code
        /// on the business object to generate.
        /// </summary>
        /// <param name="type">Interface type to resolve</param>
        /// <returns>concrete implemtation for 'type'</returns>
        public object Resolve(Type type)
        {
            //check if this type is already registered in the Kernel
            IEnumerable<IBinding> resolve = Kernel.GetBindings(type);

            //if not found in kernel generate type and register it in the Kernel for future use
            if (resolve == null || !resolve.Any())
            {
                var domainInterface = TypeHelper.RegisterDomainInterface(type, TypeHelper.GetLogicClass(type));

                if (domainInterface == null)
                    return null;

                Kernel.Bind(type).To(domainInterface);
                _bindingCache.Add(type, domainInterface);
            }

            //return an instance of T
            return Kernel.Get(type);
        }

        public Dictionary<Type, Type> GetPersistableInterfaceMapping()
        {
            return _bindingCache.Where(x=> x.Key.GetAttributes<MDataAttribute>().FirstOrDefault().GetPersist()).ToDictionary(x => x.Key, x => x.Value);
        }

        public Type GetConcreteType(Type type)
        {
            //check if this type is already registered in the Kernel
            return !_bindingCache.ContainsKey(type) ? type : _bindingCache[type];
        }

        public IResolver AutoDiscover()
        {
            TypeHelper.AutoDiscover(this);
            return this;
        }
    }
}