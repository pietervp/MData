using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.Validation;
using System.Data.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MData.Core;
using MData.EF;

namespace MData.SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            var concreteTest = Core.MData.Resolve<IAdmin>();
            var concreteTest1 = Core.MData.Resolve<ICustomer>();
            var concreteTest2 = Core.MData.Resolve<IId>();

            Core.MData.ExportAssembly();

            concreteTest.Test();
            concreteTest.TestReturnMethod();
            concreteTest.TestReturnMethodGeneric("test");
            concreteTest.TestReturnMethodWithParameters("test");

            Console.WriteLine(concreteTest.Data);

            var t = new TestContext();
            
            var customer = t.Customers.Create();
            customer.Data=  "test";
            t.Customers.Add(customer);

            var firstOrDefault = t.Customers.FirstOrDefault(x => x.Id == 1);
            var groupBy = t.Customers.GroupBy(x => x.Id).ToList();
            var queryable = t.Customers.GroupBy(x => x.Data).ToList();

            var dbEntityEntry = t.Entry(firstOrDefault);

            t.SaveChanges();

            Console.ReadLine();
        }
    }
    public class TestContext : MDataContext
    {
        public MDbSet<ICustomer> Customers { get; set; }
    }
    
    public class TestLogic : LogicBase<ICustomer>, ICustomerMethod
    {
        protected override void Init()
        {
            base.Init();
            
            EntityBase.PropertyRetrieved += (sender, args) => Console.WriteLine(args.PropertyName + " was retrieved");
        }

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

    [MDataMethod(typeof(ICustomer))]
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
