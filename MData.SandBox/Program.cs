using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using MData.Core.Base;
using MData.Core.Configuration;
using MData.EF;

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
                var customer = t.Customers.Create();
                t.Customers.Add(customer);
                
                var contact = t.Contacts.Create();
                contact.Name = string.Format("Contact {0}", contact.Id);
                customer.Contacts = contact;
                t.Contacts.Add(contact);
                
                t.SaveChanges();
            }
        }
    }
}