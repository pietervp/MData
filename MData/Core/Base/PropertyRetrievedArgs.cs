namespace MData.Core.Base
{
    public class PropertyRetrievedArgs
    {
        public PropertyRetrievedArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; set; }
    }
}