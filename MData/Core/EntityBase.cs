using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace MData.Core
{
    public class EntityBase<T> : INotifyPropertyChanged, INotifyPropertyRetrieved
    {
        private Dictionary<string, object> PropertyBag { get; set; }
        private Dictionary<string, MulticastDelegate> CustomGetters { get; set; }
        
        public EntityBase()
        {
            PropertyBag = new Dictionary<string, object>();
            CustomGetters = new Dictionary<string, MulticastDelegate>();
        }

        public void RegisterCustomGetMethod<TU>(Expression<Func<T, TU>> property, Func<TU> customerGetter)
        {
            if (!CustomGetters.ContainsKey(property.GetPropertyName()))
                CustomGetters.Add(property.GetPropertyName(), customerGetter);
            else
                CustomGetters[property.GetPropertyName()] = customerGetter;
        }

        public TU GetProperty<TU>(string name)
        {
            OnPropertyRetrieved(name);

            TU bagValue = default(TU);

            if (PropertyBag.ContainsKey(name))
                bagValue = (TU)PropertyBag[name];

            if(CustomGetters.ContainsKey(name))
                bagValue = (TU)CustomGetters[name].DynamicInvoke();

            return bagValue;
        }

        public void SetProperty<TU>(string name, TU value)
        {
            if (!PropertyBag.ContainsKey(name))
                PropertyBag.Add(name, value);
            else
                PropertyBag[name] = value;

            OnPropertyChanged(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyRetrievedEventHandler PropertyRetrieved;

        public void OnPropertyRetrieved(string propertyName)
        {
            PropertyRetrievedEventHandler handler = PropertyRetrieved;
            if (handler != null) handler(this, new PropertyRetrievedArgs(propertyName));
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}