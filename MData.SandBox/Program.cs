using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using MData.Core.Base;
using MData.Core.Configuration;

namespace MData.SandBox
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //create EF CF context
            using (DomainContext t = new DomainContext())
            {
                //all CRUD operations work
                ICustomer customer = t.Customers.Create();
                t.Customers.Add(customer);

                ICustomer firstCustomerEver = t.Customers.FirstOrDefault(x => x.Id == 1);

                t.SaveChanges();
            }
        }
    }
}