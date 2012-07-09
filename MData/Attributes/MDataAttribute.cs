using System;

namespace MData.Attributes
{
    public class MDataAttribute : Attribute
    {
        public MDataAttribute() 
            : this(null, true)
        {
        }

        public MDataAttribute(string name, bool persist = true)
        {
            Name = name;
            Persist = persist;
        }

        private bool Persist { get; set; }
        private string Name { get; set; }

        public bool GetPersist()
        {
            return Persist;
        }

        public string GetName()
        {
            return Name;
        }
    }
}