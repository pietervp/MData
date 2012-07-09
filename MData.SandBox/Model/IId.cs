using MData.Attributes;

namespace MData.SandBox
{
    [MData("IdClass", false)]
    public interface IId
    {
        int Id { get; set; }
    }
}