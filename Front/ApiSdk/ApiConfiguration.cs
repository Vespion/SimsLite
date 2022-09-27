namespace ApiSdk{

public class ApiConfiguration
{
	public string Host { get; set; } = "localhost";

	public int CertPort { get; set; } = 5574;
	
	public int AuthPort { get; set; } = 5576;

	public int SimPort { get; set; } = 5575;
} }