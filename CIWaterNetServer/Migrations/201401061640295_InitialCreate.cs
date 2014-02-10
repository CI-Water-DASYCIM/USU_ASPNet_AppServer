// Ref: http://msdn.microsoft.com/en-us/data/jj591621.aspx
// Ref: http://www.asp.net/mvc/tutorials/getting-started-with-ef-using-mvc/migrations-and-deployment-with-the-entity-framework-in-an-asp-net-mvc-application
namespace UWRL.CIWaterNetServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ServiceLog",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ServiceID = c.Int(nullable: false),
                        JobID = c.String(),
                        CallTime = c.DateTime(),
                        StartTime = c.DateTime(),
                        FinishTime = c.DateTime(),
                        RunStatus = c.String(),
                        Error = c.String(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Service", t => t.ServiceID, cascadeDelete: true)
                .Index(t => t.ServiceID);
            
            CreateTable(
                "dbo.Service",
                c => new
                    {
                        ServiceID = c.Int(nullable: false, identity: true),
                        APIName = c.String(),
                        IsAllowConcurrentRun = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ServiceID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ServiceLog", "ServiceID", "dbo.Service");
            DropIndex("dbo.ServiceLog", new[] { "ServiceID" });
            DropTable("dbo.Service");
            DropTable("dbo.ServiceLog");
        }
    }
}
