using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtConn
{
    class Program
    {
        static void Init(string connectionString)
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<Context>());
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<Context, Migrations.Configuration>(useSuppliedContext: true));
            using (var context = new Context(connectionString))
                context.Database.Initialize(false);
        }
        static void Main(string[] args)
        {
            var cs = "Data Source=localhost;Initial Catalog=efplay;User Id=sa;Password=Developer01";

            //Init();

            //var context = new Context(cs);

            var conn = new SqlConnection(cs);
            //conn.Open(); // this fails if the database doesn't exist
            var context = new Context(conn);

            try
            {
                context.Database.Initialize(false);

                foreach (var person in context.Person)
                    Console.WriteLine($"{person.FamilyName}, {person.GivenNames}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    namespace Model
    {
        public class Person
        {
            public int Id { get; set; }
            public string FamilyName { get; set; }
            public string GivenNames { get; set; }

            //public Suburb Suburb { get; set; }
        }

        public class Suburb
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    public class Context : DbContext
    {
        public Context(string cs) : base(new SqlConnection(cs), true) { }
        public Context(DbConnection conn) : base(conn, false) { }

        public DbSet<Model.Person> Person { get; set; }
        //public DbSet<Model.Suburb> Suburb { get; set; }
    }
}
