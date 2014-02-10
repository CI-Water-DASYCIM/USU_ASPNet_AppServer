namespace UWRL.CIWaterNetServer.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using UWRL.CIWaterNetServer.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<UWRL.CIWaterNetServer.DAL.ServiceContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "UWRL.CIWaterNetServer.DAL.ServiceContext";
        }

        protected override void Seed(UWRL.CIWaterNetServer.DAL.ServiceContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
            
            // This an example of deleting some of the existing recordd during the seeding process
            //var selectedServices = context.Services.ToList().FindAll(s => s.ServiceID > 8);
            //context.Services.RemoveRange(selectedServices);
            //context.SaveChanges();

            var services = new List<Service>
                {
                    new Service{ServiceID = 1, APIName = "EPADelineateController.GetShapeFiles", IsAllowConcurrentRun=true},
                    new Service{ServiceID = 2, APIName = "ShapeLatLonValuesController.GetShapeLatLonValues", IsAllowConcurrentRun=true},
                    new Service{ServiceID = 3, APIName = "CheckUEBPackageBuildStatusController.GetStatus", IsAllowConcurrentRun=true},
                    new Service{ServiceID = 4, APIName = "CheckUEBRunStatusController.GetStatus", IsAllowConcurrentRun=true},
                    new Service{ServiceID = 5, APIName = "GenerateUEBPackageController.PostUEBPackageCreate", IsAllowConcurrentRun=false},
                    new Service{ServiceID = 6, APIName = "GetUEBPackageController.GetPackageDownload", IsAllowConcurrentRun=true},
                    new Service{ServiceID = 7, APIName = "RunUEBController.PostRunUEB", IsAllowConcurrentRun=false},
                    new Service{ServiceID = 8, APIName = "UEBModelRunOutputController.GetModelRunOutput", IsAllowConcurrentRun=true}                    
                };
            
            services.ForEach(s => context.Services.AddOrUpdate(sRec => sRec.ServiceID, s));     // This will update if the ServiceID matches otherwise will insert      
            context.SaveChanges();
        }
    }
}
