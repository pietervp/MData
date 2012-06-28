using MData.Attributes;

namespace MData.SandBox
{
    [MData("Admin")]
    public interface IAdmin : ICustomer
    {
        string AdminData { get; set; }
    }
}