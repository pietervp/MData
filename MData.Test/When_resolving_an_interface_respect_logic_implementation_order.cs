using MData.Attributes;
using MData.Core;
using MData.Core.Base;
using Machine.Specifications;

namespace MData.Test
{
    [Subject(typeof(MDataKernel))]
    public class When_resolving_an_interface_respect_logic_implementation_order
    {
        private static ILocal value;

        Establish context = () => { value = MDataKernel.Resolve<ILocal>(); };

        Because of = () => { };

        It should_have_a_logic_class_oftype_TestLogicDecorated_and_not_of_TestLogic = () => { TestLogic.InitCount.ShouldEqual(0); TestLogicDecorated.InitCount.ShouldEqual(1); };

        [MData] 
        public interface ILocal{}

        [MDataLogic]
        public class TestLogicDecorated : LogicBase<ILocal>
        {
            public static int InitCount;

            protected override void Init()
            {
                InitCount++;
                base.Init();
            }
        }

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