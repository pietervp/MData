using System;
using MData.Core;

namespace MData.SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            var concreteTest = Core.MData.Resolve<ICustomer>();

            Console.WriteLine(concreteTest.Data);
            Console.ReadLine();
        }
    }

    public class TestLogic : BaseLogic<ICustomer>
    {
        protected override void Init()
        {
            base.Init();
            
            RegisterCustomGetMethod(x => x.Data, () => string.Format("this is the data: '{0}'", CurrentInstance.Id));
            EntityBase.PropertyRetrieved += (sender, args) => Console.WriteLine(args.PropertyName + " was retrieved");
        }

        public override void UnImplementedNoReturnMethodCall(string methodName, params object[] parameters)
        {
            base.UnImplementedNoReturnMethodCall(methodName, parameters);
        }

        public override TU UnImplementedMethodCall<TU>(string methodName, params object[] parameters)
        {
            return base.UnImplementedMethodCall<TU>(methodName, parameters);
        }
    }

    [MDataData("Test")]
    public interface ICustomer : IId
    {
        string Data { get; set; }
    }

    [MDataData("No")]
    public interface IId
    {
        int Id { get; set; }
    }
}
