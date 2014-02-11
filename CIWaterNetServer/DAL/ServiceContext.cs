using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using UWRL.CIWaterNetServer.Models;

// Ref: http://www.asp.net/mvc/tutorials/getting-started-with-ef-using-mvc/creating-an-entity-framework-data-model-for-an-asp-net-mvc-application
namespace UWRL.CIWaterNetServer.DAL
{
    public class ServiceContext: DbContext
    {
        public ServiceContext(): base("ServiceContext")
        {
    
        }

        public DbSet<ServiceLog> ServiceLogs { get; set; }

        public DbSet<Service> Services { get; set; }
                

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

    }
}