using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using MData.Core;
using MData.EF;

namespace MData.SandBox
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var resolver = MDataConfigurator
                .Get()
                .Recreate(false)
                .With()
                .BaseTypeForEntity<EntityBase>()
                .GetResolver();

            var concreteTest1 = resolver.Resolve<IAdmin>();
            var concreteTest = resolver.Resolve<ICustomer>();
            var concreteTest2 = resolver.Resolve<IId>();

            concreteTest.Test();
            concreteTest.TestReturnMethod();
            concreteTest.TestReturnMethodGeneric("test");
            concreteTest.TestReturnMethodWithParameters("test");

            Console.WriteLine(concreteTest.Data);

            var t = new TestContext();

            ICustomer customer = t.Customers.Create();
            t.Customers.Add(customer);

            ICustomer firstOrDefault = t.Customers.FirstOrDefault(x => x.Id == 1);
            List<IGrouping<int, ICustomer>> groupBy = t.Customers.GroupBy(x => x.Id).ToList();
            List<IGrouping<string, ICustomer>> queryable = t.Customers.GroupBy(x => x.Data).ToList();
            DbEntityEntry<ICustomer> dbEntityEntry = t.Entry(firstOrDefault);

            t.SaveChanges();

            Console.ReadLine();
        }
    }

    public class TestContext : MDbContext
    {
        public MDbSet<ICustomer> Customers { get; set; }

        public TestContext() 
        {
            
        }
    }

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

    [MData("Admin")]
    public interface IAdmin : ICustomer
    {
        string AdminData { get; set; }
    }

    [MData("Customer")]
    public interface ICustomer : IId, ICustomerMethod
    {
        string Data { get; }
    }

    [MDataMethod(typeof (ICustomer))]
    public interface ICustomerMethod
    {
        void Test();
        int TestReturnMethod();
        int TestReturnMethodWithParameters(string param);
        int TestReturnMethodGeneric<T>(T param);
    }

    [MData("IdClass")]
    public interface IId
    {
        int Id { get; set; }
    }
}