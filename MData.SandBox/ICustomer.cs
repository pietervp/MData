using MData.Attributes;

namespace MData.SandBox
{
    [MData("Customer")]
    public interface ICustomer : IId, ICustomerMethod
    {
        string Data { get; }
    }
}