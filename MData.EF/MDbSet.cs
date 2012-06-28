using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MData.EF
{
    public class MDbSet<T> : IDbSet<T> where T : class
    {
        private readonly object _internalDbSet;

        public MDbSet(DbContext context, Type concreteType)
        {
            ConcreteType = concreteType;
            _internalDbSet =
                typeof (DbContext).GetMethod("Set", new Type[] {}).MakeGenericMethod(concreteType).Invoke(context, null);
        }

        private Type ConcreteType { get; set; }

        #region IDbSet<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>) (_internalDbSet as IEnumerable).GetEnumerator();
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
            return
                _internalDbSet.GetType().GetMethod("Find", new[] {keyValues.GetType()}).Invoke(_internalDbSet,
                                                                                               new object[] {keyValues})
                as T;
        }

        public T Add(T entity)
        {
            return
                _internalDbSet.GetType().GetMethod("Add", new[] {entity.GetType()}).Invoke(_internalDbSet,
                                                                                           new object[] {entity}) as T;
        }

        public T Remove(T entity)
        {
            return
                _internalDbSet.GetType().GetMethod("Remove", new[] {entity.GetType()}).Invoke(_internalDbSet,
                                                                                              new object[] {entity}) as
                T;
        }

        public T Attach(T entity)
        {
            return
                _internalDbSet.GetType().GetMethod("Attach", new[] {entity.GetType()}).Invoke(_internalDbSet,
                                                                                              new object[] {entity}) as
                T;
        }

        public T Create()
        {
            MethodInfo createMethod =
                _internalDbSet.GetType().GetMethods().FirstOrDefault(x => x.Name == "Create" && !x.IsGenericMethod);

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
            get
            {
                return
                    new ObservableCollection<T>(
                        (_internalDbSet.GetType().GetProperty("Local").GetValue(_internalDbSet, null) as IEnumerable).
                            Cast<T>().ToList());
            }
        }

        #endregion
    }
}