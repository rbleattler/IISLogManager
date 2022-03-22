namespace IISLogManager.Core;

public class UpdateDatabaseException : Exception {
	public override string Message => "There was a problem updating the database.";
}