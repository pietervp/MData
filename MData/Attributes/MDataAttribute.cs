using System;

namespace MData.Attributes
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
}