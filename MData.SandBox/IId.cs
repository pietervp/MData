using MData.Attributes;

namespace MData.SandBox
{
    [MData("IdClass")]
    public interface IId
    {
        int Id { get; set; }
    }
}