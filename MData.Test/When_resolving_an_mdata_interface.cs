using MData.Core;
using MData.Test.Content;
using Machine.Specifications;

namespace MData.Test
{
    [Subject(typeof(MDataKernel))]
    public class When_resolving_an_mdata_interface  
    {
        private static IDecoratedInterface value;

        Establish context = () => { value = null; };

        Because of = () => { value = MDataKernel.Resolve<IDecoratedInterface>(); };

        It should_not_be_null = () => value.ShouldNotBeNull();
    }
}