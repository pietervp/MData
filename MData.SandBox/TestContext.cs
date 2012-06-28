using MData.EF;

namespace MData.SandBox
{
    public class TestContext : MDbContext
    {
        public MDbSet<ICustomer> Customers { get; set; }

        public TestContext() 
        {
            
        }
    }
}