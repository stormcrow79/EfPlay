using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncInit
{
  class Program
  {
    static void Main(string[] args)
    {
      DbConfiguration.SetConfiguration(new Data.Configuration());

      var connStr = "Data Source=localhost\\STD17;Initial Catalog=Clinical;Integrated Security=SSPI";
      var context = new Data.Context(connStr);

      var count = context.Database.SqlQuery<int>("SELECT COUNT(*) FROM Patients;").First();
      Console.WriteLine($"{count} records");
      Console.WriteLine();

      // ExecuteSqlCommandAsync causes a problem here. ExecuteSqlCommand is fine
      // problem becomes intermittent if you add BeginTransaction()
      var sname = "aaa";
      var id = 1;
      //context.Database.ExecuteSqlCommand("update Patients set surname = @p0 where id = @p1", sname, id);
      context.Database.ExecuteSqlCommandAsync("update Patients set surname = @p0 where id = @p1", sname, id).Wait();
      Console.WriteLine();

      var ps = context.Patient.ToList();
      foreach (var p in ps) Console.WriteLine($"{p.Id}: {p.Surname}, {p.FirstName}");
      Console.WriteLine();

      Console.WriteLine("done!");
    }
  }
}

namespace Data
{
  class Configuration : DbConfiguration
  {
    public Configuration() : base()
    {
      SetTransactionHandler("System.Data.SqlClient", () => new Data.TxnManager());
    }
  }

  class Context : DbContext
  {
    public Context(string connStr) : base(connStr) { }

    public DbSet<Clinical.Patient> Patient { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
    }
  }

  class TxnManager : TransactionHandler
  {
    public override string BuildDatabaseInitializationScript()
    {
      return string.Empty;
    }

    public override void Opened(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
    {
      base.Opened(connection, interceptionContext);

      foreach (var ctx in interceptionContext.DbContexts)
      {
        try
        {
          var sql = "set CONTEXT_INFO 0x414243";

          // this throws because the Context considers the async call from the mainline already in progress
          //ctx.Database.ExecuteSqlCommand(sql);

          // this doesn't throw since it bypasses the async checks in the Context
          var cmd = ctx.Database.Connection.CreateCommand();
          cmd.CommandText = sql;
          cmd.ExecuteNonQuery();

          //ctx.Database.SqlQuery<int>(sql).First(); // ***
        }
        catch (Exception ex)
        {
          Console.WriteLine($"init error: {ex.Message}");
          throw;
        }
      }
    }
  }
}

namespace Clinical
{
  class Patient
  {
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string Surname { get; set; }
    public DateTime DateOfBirth { get; set; }
  }
}