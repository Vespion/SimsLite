namespace SimServer.ConfigMap;

public class Certificates
{
	public string? JWTSigningCertificate { get; set; }
	
	public string? JWTSigningCertificateFile { get; set; }
	
	public string? CurveCertificate { get; set; }
	
	public string? CurveCertificateFile { get; set; }
}