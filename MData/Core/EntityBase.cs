using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace MData.Core
{
    public class EntityBase : INotifyPropertyChanged, INotifyPropertyRetrieved//, IEntityBase<T>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyRetrievedEventHandler PropertyRetrieved;

        internal void OnPropertyRetrieved(string propertyName)
        {
            PropertyRetrievedEventHandler handler = PropertyRetrieved;
            if (handler != null) handler(this, new PropertyRetrievedArgs(propertyName));
        }

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}