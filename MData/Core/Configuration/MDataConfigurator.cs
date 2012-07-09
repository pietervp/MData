using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BLToolkit.Common;

namespace MData.Core.Configuration
{
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
            if(Assemblies == null || !Assemblies.Any())
                Assemblies = new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() }.Union(Assembly.GetExecutingAssembly().GetModules().Select(x => x.Assembly).ToArray());

            return this;
        }

        IBaseTypeConfig IBaseTypeConfig.BaseTypeForEntity<TEntityBase>()
        {
            EntityType = typeof(TEntityBase);
            return this;
        }

        public static IResolver GetDefaultResolver()
        {
            return _mDataKernel ?? Get().Recreate(true).With().GetResolver().AutoDiscover();
        }

        public IResolver GetResolver()
        {
            if (EntityType == null)
                EntityType = typeof(Base.EntityBase);

            _mDataKernel = new MDataKernel(this);
            return _mDataKernel;
        }
    }
}