using System.Globalization;
using MData.Core;
using MData.Core.Base;

namespace MData.SandBox
{
    public class TestLogic : LogicBase<ICustomer>
    {
        #region ICustomerMethod Members

        //public void Test()
        //{
        //    SetProperty(x=> x.Data, "Test");
        //    Console.WriteLine("HASH:" + GetProperty(x => x.GetHashCode()));
        //}

        //public int TestReturnMethod()
        //{
        //    return 0;
        //}

        //public int TestReturnMethodWithParameters(string param)
        //{
        //    return 0;
        //}

        //public int TestReturnMethodGeneric<T>(T param)
        //{
        //    return 0;
        //}

        #endregion

        protected override void Init()
        {
            base.Init();

            //defining a readonly/calculated property
            RegisterCustomGetMethod(x=> x.Data, () => CurrentInstance.Id.ToString(CultureInfo.InvariantCulture));
        }
    }
}