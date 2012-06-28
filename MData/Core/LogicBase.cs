using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MData.Core
{
    public class LogicBase
    {
        protected Dictionary<string, object> PropertyBag { get; set; }
        protected Dictionary<string, MulticastDelegate> CustomGetters { get; set; }

        public LogicBase()
        {
            CustomGetters = new Dictionary<string, MulticastDelegate>();
            PropertyBag = new Dictionary<string, object>();
        }

        public virtual TU GetProperty<TU>(string name)
        {

            TU bagValue = default(TU);

            if (PropertyBag.ContainsKey(name))
                bagValue = (TU) PropertyBag[name];

            if (CustomGetters.ContainsKey(name))
                bagValue = (TU) CustomGetters[name].DynamicInvoke();

            return bagValue;
        }

        public virtual void SetProperty<TU>(string name, TU value)
        {
            if (!PropertyBag.ContainsKey(name))
                PropertyBag.Add(name, value);
            else
                PropertyBag[name] = value;
        }

        public virtual void UnImplementedNoReturnMethodCall(string methodName, params object[] parameters)
        {
            //Console.WriteLine("Calling 'UnImplementedNoReturnMethodCall'\n\t => Method: {0},\n\t Parameters: {1}", methodName, parameters.Select(x => x == null ? "null" : x.ToString()).Aggregate((x, y) => x + ", " + y));   
        }

        public virtual TU UnImplementedMethodCall<TU>(string methodName, params object[] parameters)
        {
            //Console.WriteLine("Calling 'UnImplementedMethodCall<{2}>'\n\t => Method: {0},\n\t Parameters: {1}", methodName, parameters.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y), typeof(T).Name);   
            return default(TU);
        }

        protected virtual void Init()
        {
            //Console.WriteLine("Init on {0}", GetType().FullName);
        }
    }

    public class LogicBase<T> : LogicBase
    {
        private T _currentInstance;

        public EntityBase EntityBase
        {
            get { return CurrentInstance as EntityBase; }
        }

        public T CurrentInstance
        {
            get { return _currentInstance; }
            set
            {
                _currentInstance = value;
                Init();
            }
        }

        public override TU GetProperty<TU>(string name)
        {
            EntityBase.OnPropertyRetrieved(name);
            return base.GetProperty<TU>(name);
        }

        public override void SetProperty<TU>(string name, TU value)
        {
            base.SetProperty(name, value);
            EntityBase.OnPropertyChanged(name);
        }
        
        public TU GetProperty<TU>(Expression<Func<T, TU>> property)
        {
            var memberExpression = property.Body as MemberExpression;

            if (memberExpression != null)
                return GetProperty<TU>(memberExpression.Member.Name);

            return property.Compile().Invoke(CurrentInstance);
        }

        public void SetProperty<TU>(Expression<Func<T, TU>> property, TU value)
        {
            var memberExpression = property.Body as MemberExpression;

            if (memberExpression != null)
                SetProperty(memberExpression.Member.Name, value);
        }

        public void RegisterCustomGetMethod<TU>(Expression<Func<T, TU>> property, Func<TU> customerGetter)
        {
            if (!CustomGetters.ContainsKey(property.GetPropertyName()))
                CustomGetters.Add(property.GetPropertyName(), customerGetter);
            else
                CustomGetters[property.GetPropertyName()] = customerGetter;
        }
    }
}