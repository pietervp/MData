using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MData.Core
{
    public class LogicBase<T> 
    {
        public LogicBase()
        {
            CustomGetters = new Dictionary<string, MulticastDelegate>();
            PropertyBag = new Dictionary<string, object>();
        }
        
        private T _currentInstance;

        private Dictionary<string, object> PropertyBag { get; set; }
        private Dictionary<string, MulticastDelegate> CustomGetters { get; set; }

        public EntityBase EntityBase { get { return CurrentInstance as EntityBase; } }
        public T CurrentInstance
        {
            get { return _currentInstance; }
            set { _currentInstance = value; Init(); }
        }

        public TU GetProperty<TU>(string name)
        {
            EntityBase.OnPropertyRetrieved(name);

            var bagValue = default(TU);

            if (PropertyBag.ContainsKey(name))
                bagValue = (TU)PropertyBag[name];

            if (CustomGetters.ContainsKey(name))
                bagValue = (TU)CustomGetters[name].DynamicInvoke();

            return bagValue;
        }

        public void SetProperty<TU>(string name, TU value)
        {
            if (!PropertyBag.ContainsKey(name))
                PropertyBag.Add(name, value);
            else
                PropertyBag[name] = value;

            EntityBase.OnPropertyChanged(name);
        }

        public void RegisterCustomGetMethod<TU>(Expression<Func<T, TU>> property, Func<TU> customerGetter)
        {
            if (!CustomGetters.ContainsKey(property.GetPropertyName()))
                CustomGetters.Add(property.GetPropertyName(), customerGetter);
            else
                CustomGetters[property.GetPropertyName()] = customerGetter;
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
}

