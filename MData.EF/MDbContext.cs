using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Objects;
using System.Linq;
using System.Reflection;
using MData.Core;
using MData.Core.Base;
using MData.Core.Configuration;

namespace MData.EF
{
    public class MDbContext : DbContext
    {
        private static readonly IResolver Resolver;
        private bool _wasInitialized;

        static MDbContext()
        {
            Resolver = MDataConfigurator.GetDefaultResolver();
        }

        /// <summary>
        /// Returns a non-generic DbSet instance for access to entities of the given type in the context,
        ///                 the ObjectStateManager, and the underlying store.
        /// </summary>
        /// <param name="entityType">The type of entity for which a set should be returned.</param>
        /// <returns>
        /// A set for the given entity type.
        /// </returns>
        /// <remarks>
        /// See the MDbSet class for more details.
        /// </remarks>
        public new DbSet Set(Type entityType)
        {
            return base.Set(Resolver.GetPersistableInterfaceMapping()[entityType]);
        }

        /// <summary>
        /// Returns a MDbSet instance for access to entities of the given type in the context,
        ///                 the ObjectStateManager, and the underlying store.
        /// </summary>
        /// <remarks>
        /// See the MDbSet class for more details.
        /// </remarks>
        /// <typeparam name="TEntity">The type entity for which a set should be returned.</typeparam>
        /// <returns>
        /// A set for the given entity type.
        /// </returns>
        public new MDbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            return new MDbSet<TEntity>(this, Resolver.GetConcreteType(typeof(TEntity)));
        }

        #region Constructors

        protected MDbContext()
        {
            Init();
        }

        public MDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Init();
        }

        public MDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Init();
        }

        public MDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Init();
        }

        public MDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Init();
        }
        
        protected MDbContext(DbCompiledModel model)
            : base(model)
        {
            Init();
        }

        public MDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Init();
        }

        #endregion

        #region Initialization

        private void Init()
        {
            if (_wasInitialized)
                return;

            _wasInitialized = true;

            foreach (var source in GetDbSetPropertiesToInitialize())
                InitializeDbSetProperty(source);
        }

        private IEnumerable<PropertyInfo> GetDbSetPropertiesToInitialize()
        {
            return from prop in GetType().GetProperties()
                   let proptype = prop.PropertyType
                   where proptype.IsGenericType
                         && proptype.GetGenericTypeDefinition() == typeof(MDbSet<>)
                   select prop;
        }

        private void InitializeDbSetProperty(PropertyInfo source)
        {
            var genericArgument = source.PropertyType.GetGenericArguments()[0];
            var genericSet = typeof(MDbSet<>).MakeGenericType(genericArgument);
            var genericSetConstructor = genericSet.GetConstructor(new[] { typeof(DbContext), typeof(Type) });

            if (genericSetConstructor == null)
                return;

            var newDbSet = genericSetConstructor.Invoke(new object[]
                                                            {
                                                                this, Resolver.GetConcreteType(genericArgument)
                                                            });

            //set value of the property to the newly created dbset
            source.SetValue(this, newDbSet, null);
        }
        
        protected virtual void OnModelCreatingEx(DbModelBuilder modelBuilder)
        {
            
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected sealed override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder = new MDbModelBuilder(modelBuilder);

            base.OnModelCreating(modelBuilder);
            
            foreach (var mapping in Resolver.GetPersistableInterfaceMapping())
            {
                var entityTypeConfiguration = modelBuilder.GetType().GetMethod("Entity").MakeGenericMethod(mapping.Value).Invoke(modelBuilder, null);
                var toTableMethod = entityTypeConfiguration.GetType().GetMethod("ToTable", new[] { typeof(string) });
                var hasSetNameMethod = entityTypeConfiguration.GetType().GetMethod("HasEntitySetName", new[] { typeof(string) });
                
                toTableMethod.Invoke(entityTypeConfiguration, new object[] { mapping.Key.Name });
                hasSetNameMethod.Invoke(entityTypeConfiguration, new object[] { string.Format("{0}Set", mapping.Key.Name) });
            }

            OnModelCreatingEx(modelBuilder);
        }

        #endregion
    }

    public class MDbModelBuilder : DbModelBuilder
    {
        private DbModelBuilder _internalBuilder;
        private IResolver _defaultResolver;
        
        public MDbModelBuilder(DbModelBuilder modelBuilder)
        {
            _defaultResolver = MDataConfigurator.GetDefaultResolver();
            _internalBuilder = modelBuilder;
        }

        public override DbModelBuilder Ignore<T>()
        {
            return Ignore(new[] { typeof(T) });
        }

        public override DbModelBuilder Ignore(IEnumerable<Type> types)
        {
            return _internalBuilder.Ignore(types.Select(x => _defaultResolver.GetConcreteType(x)));
        }

        public override  EntityTypeConfiguration<TEntityType> Entity<TEntityType>()
        {
            return _internalBuilder.Entity<TEntityType>();
        }

        public override  ComplexTypeConfiguration<TComplexType> ComplexType<TComplexType>()
        {
            return _internalBuilder.ComplexType<TComplexType>();
        }

        public override DbModel Build(DbConnection providerConnection)
        {
            return _internalBuilder.Build(providerConnection);
        }

        public override DbModel Build(DbProviderInfo providerInfo)
        {
            return _internalBuilder.Build(providerInfo);
        }

        public void AddConfiguration<T>(EntityTypeConfiguration<T> configuration) where T : class
        {
            Configurations.Add(configuration);
        }

        public override ConventionsConfiguration Conventions
        {
            get { return _internalBuilder.Conventions; }
        }

        public override ConfigurationRegistrar Configurations
        {
            get { return _internalBuilder.Configurations; }
        }
    }
}