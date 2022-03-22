using System.ComponentModel;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql;

namespace IISLogManager.Core;

public class IISLogManagerContext : DbContext {
	public DbSet<IISLogObject> IISLogObjectSet { get; set; }
	public string ConnectionString { get; init; }
	private DatabaseProvider DatabaseProvider { get; init; }
	public string TableName { get; set; } = "IISLogs";

	//TODO: Default Implementations where a local database is created 

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<IISLogObject>(entity => {
			entity.ToTable(TableName);
			entity.HasKey("UniqueId");
		});
	}

	protected override void OnConfiguring(DbContextOptionsBuilder options) {
		switch (DatabaseProvider) {
			case DatabaseProvider.SQL:
				throw new NotImplementedException(
					"SQL (Server) Provider is not yet implemented.");
			case DatabaseProvider.Sqlite:
				options.UseSqlite(ConnectionString);
				break;
			case DatabaseProvider.MySQL:
				var mySqlConnection = new MySqlConnection(ConnectionString);
				options.UseMySql(ConnectionString, ServerVersion.AutoDetect(mySqlConnection));
				throw new WarningException("MySQL Provider implementation is unstable. Use at your own risk!");
			case DatabaseProvider.PostgreSQL:
				throw new NotImplementedException(
					"PostgreSQL Provider is not yet implemented.");
			case DatabaseProvider.Oracle:
				throw new NotImplementedException(
					"Oracle Provider is not yet implemented.");
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	// protected override void OnModelCreating(ModelBuilder modelBuilder) {
	// 	modelBuilder.Entity<IISLogObject>();
	// 	// base.OnModelCreating(modelBuilder);
	// }

	public IISLogManagerContext(DatabaseProvider databaseProvider, string connectionString,
		string? tableName) {
		tableName = string.IsNullOrWhiteSpace(tableName) ? "IISLogs" : tableName;
		// if ( string.IsNullOrWhiteSpace(tableName) ) tableName = "IISLogs";
		DatabaseProvider = databaseProvider;
		ConnectionString = connectionString;
		TableName = tableName;
	}
}