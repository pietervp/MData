using MData.Core.Base;

namespace MData.Core.Configuration
{
    public interface IBaseTypeConfig
    {
        IBaseTypeConfig BaseTypeForEntity<TEntityBase>() where TEntityBase : EntityBase;
        IResolver GetResolver();
    }
}