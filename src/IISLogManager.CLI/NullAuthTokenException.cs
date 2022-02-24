namespace IISLogManager.CLI;

public class NullAuthTokenException : ArgumentNullException {
	public new string Message = "The authorization token was empty";
	public NullAuthTokenException() { }
}