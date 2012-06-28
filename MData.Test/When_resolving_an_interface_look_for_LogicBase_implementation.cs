using MData.Core;
using Machine.Specifications;

namespace MData.Test
{
    [Subject(typeof(MDataKernel))]
    public class When_resolving_an_interface_look_for_LogicBase_implementation
    {
        private static ILocal value;

        Establish context = () => { value = MDataKernel.Resolve<ILocal>(); };

        Because of = () => { };

        It should_have_a_logic_class_oftype_TestLogic = () => TestLogic.InitCount.ShouldEqual(1);

        [MData]
        public interface ILocal { }

        public class TestLogic : LogicBase<ILocal>
        {
            public static int InitCount;

            protected override void Init()
            {
                InitCount++;
                base.Init();
            }
        }
    }
}