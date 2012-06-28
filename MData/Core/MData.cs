using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Activation;

namespace MData.Core
{
    /// <summary>
    /// MData is the entry point of the framework.
    /// </summary>
    public class MData
    {
        private static readonly Dictionary<Type, Type> BindingCache;
        private static TypeCreator TypeHelper { get; set; }
        private static StandardKernel Kernel { get; set; }

        static MData()
        {
            Kernel = new StandardKernel();
            TypeHelper = new TypeCreator();
            BindingCache = new Dictionary<Type, Type>();
        }

        public static void ExportAssembly()
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
        public static T Resolve<T>(Type baseType = null) where T : class
        {
            //check if this type is already registered in the Kernel
            var resolve = Kernel.GetBindings(typeof(T));

            //if not found in kernel generate type and register it in the Kernel for future use
            if (resolve == null || !resolve.Any())
            {
                var domainInterface = TypeHelper.RegisterDomainInterface(typeof (T), TypeHelper.GetLogicClass(typeof (T)));
                Kernel.Bind<T>().To(domainInterface);
                BindingCache.Add(typeof(T), domainInterface);
            }

            //return an instance of T
            return Kernel.Get<T>();
        }

        public static Dictionary<Type, Type> GetInterfaceMapping()
        {
            return BindingCache.ToDictionary(x => x.Key, x => x.Value);
        }

        public static Type GetConcreteType(Type type)
        {
            //check if this type is already registered in the Kernel
            return !BindingCache.ContainsKey(type) ? type : BindingCache[type];
        }
    }
}