using MData.Attributes;

namespace MData.Test.Content
{
    [MData]
    public interface IDecoratedInterface
    {
        string Data { get; set; }
    }
}