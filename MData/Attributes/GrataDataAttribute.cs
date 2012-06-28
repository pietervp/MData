using System;

namespace MData
{
    public class MDataAttribute : Attribute
    {
        private string Name { get; set; }

        public MDataAttribute()
        {
        }

        public MDataAttribute(string name)
        {
            Name = name;
        }

        public string GetName()
        {
            return  Name;
        }
    }

    public class MDataMethodAttribute : Attribute
    {
        private readonly Type _linkedInterfaceType = null;

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