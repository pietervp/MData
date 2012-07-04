using System;
using MData.Attributes;
using MData.Core.Base;

namespace MData.SandBox
{
    [MData("Customer")]
    public interface ICustomer : IId, ICustomerMethod
    {
        int Id { get; set; }
        string Name { get; set; }
        string Notes { get; set; }
        bool IsActive { get; set; }
        DateTime CreatedOn { get; set; }
        DateTime ModifiedOn { get; set; }

        void Active();
        void DeActivate();
    }
}