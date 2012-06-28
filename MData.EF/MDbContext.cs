using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace MData.EF
{
    public class MDbContext : DbContext
    {
        public MDbContext()
        {
            foreach (var source in this.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(MDbSet<>)))
            {
                var genericArgument = source.PropertyType.GetGenericArguments()[0];
                var genericSet = typeof(MDbSet<>).MakeGenericType(genericArgument);
                var genericSetConstructor = genericSet.GetConstructor(new[] { typeof(DbContext), typeof(Type) });

                if (genericSetConstructor != null)
                    source.SetValue(this, genericSetConstructor.Invoke(new object[] { this, Core.MData.GetConcreteType(genericArgument) }), null);
            }
        }

        public new DbSet Set(Type entityType)
        {
            return base.Set(Core.MData.GetInterfaceMapping()[entityType]);
        }

        public new MDbSet<T> Set<T>() where T : class
        {
            return new MDbSet<T>(this, Core.MData.GetConcreteType(typeof(T)));
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var mapping in Core.MData.GetInterfaceMapping())
            {
                var entityTypeConfiguration = modelBuilder.GetType().GetMethod("Entity").MakeGenericMethod(mapping.Value).Invoke(modelBuilder, null);
                var toTableMethod = entityTypeConfiguration.GetType().GetMethod("ToTable", new[] { typeof(string) });
                var hasSetNameMethod = entityTypeConfiguration.GetType().GetMethod("HasEntitySetName", new[] { typeof(string) });

                toTableMethod.Invoke(entityTypeConfiguration, new object[] { mapping.Key.Name });
                hasSetNameMethod.Invoke(entityTypeConfiguration, new object[] { string.Format("{0}Set", mapping.Key.Name) });
            }
        }
    }
}
