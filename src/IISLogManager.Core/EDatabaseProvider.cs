namespace IISLogManager.Core;

public enum DatabaseProvider {
	SQL,
	Sqlite,
	MySQL, // Not Implemented due to incompatibility with NetStandard 2.0
	PostgreSQL,
	Oracle
}