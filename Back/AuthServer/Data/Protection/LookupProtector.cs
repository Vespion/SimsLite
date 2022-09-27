using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Protection;

public class LookupProtector : ILookupProtector
{
	private readonly IPersistedDataProtector _rootProtector;

	public LookupProtector(IDataProtectionProvider rootProtector)
	{
		_rootProtector = rootProtector.CreateProtector("Identity", "Lookup") as IPersistedDataProtector ?? throw new NotSupportedException("Unable to constructed persisted data protector");
	}
	
	/// <inheritdoc />
	public string Protect(string keyId, string data)
	{
		return _rootProtector.Protect(data);
	}

	/// <inheritdoc />
	public string Unprotect(string keyId, string data)
	{
		var dataBytes = Convert.FromBase64String(data);
		var plainBytes = _rootProtector.Unprotect(dataBytes);
		return PersonalDataProtector.SecureUtf8Encoding.GetString(plainBytes);
	}
}