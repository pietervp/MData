using System;
using MData.Core;
using Machine.Specifications;

namespace MData.Test
{
    [Subject(typeof(MDataKernel))]
    public class When_resolving_an_interface_respect_logic_implementation_order_fail_if_impossible
    {
        private static ILocalDomain value;
        static Exception Exception;

        Establish context = () => {  };

        Because of = () => Exception = Catch.Exception(()=> value = MDataKernel.Resolve<ILocalDomain>());

        It should_fail = () => Exception.ShouldBeOfType<FoundMultipleLogicImplementationsException>();
	
        [MData]
        public interface ILocalDomain { }

        [MDataLogic]
        public class TestLogicOne : LogicBase<ILocalDomain>
        {
            public static int InitCount;

            protected override void Init()
            {
                InitCount++;
                base.Init();
            }
        }

        //all other tests fail
        //[MDataLogic]
        public class TestLogicTwo : LogicBase<ILocalDomain>
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