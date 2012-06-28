using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using MData.Core;
using MData.EF;

namespace MData.SandBox
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var concreteTest = MDataKernel.Resolve<IAdmin>();
            var concreteTest1 = MDataKernel.Resolve<ICustomer>();
            var concreteTest2 = MDataKernel.Resolve<IId>();

            MDataKernel.ExportAssembly();

            concreteTest.Test();
            concreteTest.TestReturnMethod();
            concreteTest.TestReturnMethodGeneric("test");
            concreteTest.TestReturnMethodWithParameters("test");

            Console.WriteLine(concreteTest.Data);

            var t = new TestContext();

            ICustomer customer = t.Customers.Create();
            customer.Data = "test";
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
    }

    public class TestLogic : LogicBase<ICustomer>, ICustomerMethod
    {
        #region ICustomerMethod Members

        public void Test()
        {
        }

        public int TestReturnMethod()
        {
            return 0;
        }

        public int TestReturnMethodWithParameters(string param)
        {
            return 0;
        }

        public int TestReturnMethodGeneric<T>(T param)
        {
            return 0;
        }

        #endregion

        protected override void Init()
        {
            base.Init();

            EntityBase.PropertyRetrieved += (sender, args) => Console.WriteLine(args.PropertyName + " was retrieved");
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
        string Data { get; set; }
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