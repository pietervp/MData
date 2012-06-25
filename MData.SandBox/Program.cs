using System;
using MData.Core;

namespace MData.SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            var concreteTest = Core.MData.Resolve<ITest>(typeof(EntityBase<ITest>));

            concreteTest.Data = "MData";

            Console.WriteLine("This is data: {0}", concreteTest.Data);
            
            concreteTest.Test("Test", "1", "2");
            
            Console.WriteLine(concreteTest.Data);
            Console.ReadLine();
        }
    }

    public class TestLogic : BaseLogic<ITest>
    {
        protected override void Init()
        {
            base.Init();

            EntityBase.RegisterCustomGetMethod(x => x.Data, () => string.Format("this is the data: '{0}'", CurrentInstance.Id));
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
    public interface ITest : IId
    {
        string Data { get; set; }
        void Test(params string[] parameters);
    }

    [MDataData("No")]
    public interface IId
    {
        int Id { get; set; }
        int MethodThree(int data);
        void Gener<T>(T parameter, string test);
        void Gener<T>(T parameter, int index);
    }

    [MDataData("SimpleTest")]
    public interface ISimpleTest
    {
        void Method();
    }

}
