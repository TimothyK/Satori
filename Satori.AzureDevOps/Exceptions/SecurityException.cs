namespace Satori.AzureDevOps.Exceptions;

public class SecurityException(string message, Exception innerException) : HttpRequestException(message, innerException);