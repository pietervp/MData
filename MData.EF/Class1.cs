using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MData.EF
{
    public class MDbSet<T> : IDbSet<T> where T : class
    {
        private readonly object _internalDbSet;
        private DbContext Context { get; set; }
        private Type ConcreteType { get; set; }

        public MDbSet(DbContext context, Type concreteType)
        {
            Context = context;
            ConcreteType = concreteType;
            _internalDbSet = typeof(DbContext).GetMethod("Set", new Type[] { }).MakeGenericMethod(concreteType).Invoke(context, null);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)(_internalDbSet as IEnumerable).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (_internalDbSet as IEnumerable).GetEnumerator();
        }

        public Expression Expression
        {
            get { return (_internalDbSet as IQueryable).Expression; }
        }

        public Type ElementType
        {
            get { return ConcreteType; }
        }

        public IQueryProvider Provider
        {
            get { return (_internalDbSet as IQueryable).Provider; }
        }

        public T Find(params object[] keyValues)
        {
            return _internalDbSet.GetType().GetMethod("Find", new Type[] { keyValues.GetType() }).Invoke(_internalDbSet, new object[] { keyValues }) as T;
        }

        public T Add(T entity)
        {
            return _internalDbSet.GetType().GetMethod("Add", new Type[] { entity.GetType() }).Invoke(_internalDbSet, new object[] { entity }) as T;
        }

        public T Remove(T entity)
        {
            return _internalDbSet.GetType().GetMethod("Remove", new Type[] { entity.GetType() }).Invoke(_internalDbSet, new object[] { entity }) as T;
        }

        public T Attach(T entity)
        {
            return _internalDbSet.GetType().GetMethod("Attach", new Type[] { entity.GetType() }).Invoke(_internalDbSet, new object[] { entity }) as T;
        }

        public T Create()
        {
            var createMethod = _internalDbSet.GetType().GetMethods().FirstOrDefault(x => x.Name == "Create" && !x.IsGenericMethod);
            
            if (createMethod != null)
                return createMethod.Invoke(_internalDbSet, null) as T;
            
            return null;
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<T> Local
        {
            get { return new ObservableCollection<T>((_internalDbSet.GetType().GetProperty("Local").GetValue(_internalDbSet, null) as IEnumerable).Cast<T>().ToList()); }
        }
    }

    public class MDataContext : DbContext
    {
        public MDataContext()
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

            foreach (var mapping in MData.Core.MData.GetInterfaceMapping())
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
