using MData.Attributes;
using MData.Core;
using MData.Core.Base;
using Machine.Specifications;

namespace MData.Test
{
    [Subject(typeof(MDataKernel))]
    public class When_resolving_an_interface_wich_has_parent_interfaces_look_for_correct_LogicBase_implementation_extended
    {
        private static ILocalEvolved value;

        Establish context = () => { value = MDataKernel.Resolve<ILocalEvolved>(); };

        Because of = () => { };

        It should_call_correct_logic_base_and_return_OK = () => { value.Data.ShouldEqual("OK"); value.DataEvolved.ShouldEqual("OK"); };


        [MData]
        public interface ILocalEvolved : ILocal { string DataEvolved { get; set; } }

        [MData]
        public interface ILocal { string Data { get; set; } }

        public class TestLogicEvolved : LogicBase<ILocalEvolved>
        {
            public static int InitCount;

            protected override void Init()
            {
                InitCount++;
                base.Init();

                RegisterCustomGetMethod(x => x.Data, () => "NOK");
                RegisterCustomGetMethod(x => x.DataEvolved, () => "OK");
            }
        }

        public class TestLogic : LogicBase<ILocal>
        {
            public static int InitCount;

            protected override void Init()
            {
                InitCount++;
                base.Init();

                RegisterCustomGetMethod(x => x.Data, () => "OK");
            }
        }
    }
}