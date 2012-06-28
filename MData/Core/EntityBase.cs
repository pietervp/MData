using System.ComponentModel;

namespace MData.Core
{
    public class EntityBase : INotifyPropertyChanged, INotifyPropertyRetrieved
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region INotifyPropertyRetrieved Members

        public event PropertyRetrievedEventHandler PropertyRetrieved;

        #endregion

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