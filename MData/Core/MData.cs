using System;
using System.Linq;
using Ninject;

namespace MData.Core
{
    /// <summary>
    /// MData is the entry point of the framework.
    /// </summary>
    public class MData
    {
        private static TypeCreator TypeHelper { get; set; }
        private static StandardKernel Kernel { get; set; }

        static MData()
        {
            Kernel = new StandardKernel();
            TypeHelper = new TypeCreator();
        }

        /// <summary>
        /// Resolves an interface to a concrete type, by generating a proxy class that implements this interface.
        /// Resolve method will look for a BaseLogic Implementor in the available assemblies, to execute custom code
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
                Kernel.Bind<T>().To(TypeHelper.RegisterDomainInterface(typeof(T), TypeHelper.GetLogicClass(typeof(T))));
            
            //return an instance of T
            return Kernel.Get<T>();
        }
    }
}