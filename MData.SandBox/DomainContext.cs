using System.Data.Entity.ModelConfiguration;
using MData.EF;

namespace MData.SandBox
{
    public class DomainContext : MDbContext
    {
        public MDbSet<ICustomer> Customers { get; set; }
        public MDbSet<IContact> Contacts { get; set; }

        protected override void OnModelCreatingEx(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            base.OnModelCreatingEx(modelBuilder);

            //modelBuilder.Configurations.Add(new CustomerConfig());
        }
    }

    //public class CustomerConfig : EntityTypeConfiguration<ICustomer>
    //{
    //    public CustomerConfig()
    //    {
    //        Property(x => x.Data).HasColumnName("ByeBye");
    //        ToTable("HolyCustomers");
    //    }
    //}
}