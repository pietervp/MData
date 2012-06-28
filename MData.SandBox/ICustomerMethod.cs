using MData.Attributes;

namespace MData.SandBox
{
    [MDataMethod(typeof (ICustomer))]
    public interface ICustomerMethod
    {
        void Test();
        int TestReturnMethod();
        int TestReturnMethodWithParameters(string param);
        int TestReturnMethodGeneric<T>(T param);
    }
}