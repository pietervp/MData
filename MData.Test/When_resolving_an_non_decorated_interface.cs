using MData.Core;
using Machine.Specifications;

namespace MData.Test
{
    [Subject(typeof(MDataKernel))]
    public class When_resolving_an_non_decorated_interface
    {
        private static INonDecoratedInterface value;

        Establish context = () => { value = null; };

        Because of = () => { value = MDataKernel.Resolve<INonDecoratedInterface>(); };

        It should_return_null = () => value.ShouldBeNull();
    }
}