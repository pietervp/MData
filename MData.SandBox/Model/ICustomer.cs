using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MData.Attributes;

namespace MData.SandBox
{
    [MData("Customer")]
    public interface ICustomer : IId, ICustomerMethod, IAuditable
    {
        string Name { get; set; }
        string Notes { get; set; }

        IContact Contacts { get; set; }

        bool IsActive { get; set; }
        void Active();
        void DeActivate();
    }
    
    [MData("Auditable", false)]
    public interface IAuditable
    {
        DateTime CreatedOn { get; set; }
        DateTime ModifiedOn { get; set; }
    }

    [MData("Contact")]
    public interface IContact : IId
    {
        string Name { get; set; }
        ICollection<ICustomer> Customers { get; set; }
    }
}