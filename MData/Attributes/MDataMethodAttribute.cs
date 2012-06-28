using System;

namespace MData.Attributes
{
    public class MDataMethodAttribute : Attribute
    {
        private readonly Type _linkedInterfaceType;

        public MDataMethodAttribute(Type linkedInterfaceType)
        {
            _linkedInterfaceType = linkedInterfaceType;
        }

        public Type GetLinkedInterfaceType()
        {
            return _linkedInterfaceType;
        }
    }
}