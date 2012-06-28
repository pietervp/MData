using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Linq;
using System.Reflection;
using MData.Core;

namespace MData.EF
{
    public class MDbContext : DbContext
    {
        private static readonly IResolver Resolver;
        private bool wasInitialized;

        static MDbContext()
        {
            Resolver = MDataConfigurator.GetDefaultResolver();
        }

        public MDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        {
            Init();
        }
        
        public MDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
        {
            Init();
        }

        public MDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext) : base(objectContext, dbContextOwnsObjectContext)
        {
            Init();
        }

        public MDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        {
            Init();
        }

        protected MDbContext()
        {
            Init();
        }

        protected MDbContext(DbCompiledModel model) : base(model)
        {
            Init();
        }

        public MDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            Init();
        }


        private void Init()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;

            foreach (
                PropertyInfo source in
                    GetType().GetProperties().Where(
                        x =>
                        x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(MDbSet<>)))
            {
                Type genericArgument = source.PropertyType.GetGenericArguments()[0];
                Type genericSet = typeof(MDbSet<>).MakeGenericType(genericArgument);
                ConstructorInfo genericSetConstructor =
                    genericSet.GetConstructor(new[] { typeof(DbContext), typeof(Type) });

                if (genericSetConstructor != null)
                    source.SetValue(this,
                                    genericSetConstructor.Invoke(new object[]
                                                                     {
                                                                         this, Resolver.GetConcreteType(genericArgument)
                                                                     }), null);
            }
        }

        public new DbSet Set(Type entityType)
        {
            return base.Set(Resolver.GetInterfaceMapping()[entityType]);
        }

        public new MDbSet<T> Set<T>() where T : class
        {
            return new MDbSet<T>(this, Resolver.GetConcreteType(typeof(T)));
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var mapping in Resolver.GetInterfaceMapping())
            {
                object entityTypeConfiguration =
                    modelBuilder.GetType().GetMethod("Entity").MakeGenericMethod(mapping.Value).Invoke(modelBuilder,
                                                                                                       null);
                MethodInfo toTableMethod = entityTypeConfiguration.GetType().GetMethod("ToTable",
                                                                                       new[] {typeof (string)});
                MethodInfo hasSetNameMethod = entityTypeConfiguration.GetType().GetMethod("HasEntitySetName",
                                                                                          new[] {typeof (string)});

                toTableMethod.Invoke(entityTypeConfiguration, new object[] {mapping.Key.Name});
                hasSetNameMethod.Invoke(entityTypeConfiguration,
                                        new object[] {string.Format("{0}Set", mapping.Key.Name)});
            }
        }
    }
}