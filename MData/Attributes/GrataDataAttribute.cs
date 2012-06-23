using System;

namespace MData
{
    public class MDataDataAttribute : Attribute
    {
        public string Name { get; set; }

        public MDataDataAttribute(string name = null)
        {
            Name = name;
        }
    }
}