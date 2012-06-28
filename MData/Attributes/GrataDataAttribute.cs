using System;

namespace MData
{
    public class MDataAttribute : Attribute
    {
        public MDataAttribute()
        {
        }

        public MDataAttribute(string name)
        {
            Name = name;
        }

        private string Name { get; set; }

        public string GetName()
        {
            return Name;
        }
    }

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