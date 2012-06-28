using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using MData.Core;
using MData.Core.Base;
using MData.Core.Configuration;

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
}