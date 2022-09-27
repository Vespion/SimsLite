using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Protection;

public class PersonalDataProtector : IPersonalDataProtector
{
	private readonly IPersistedDataProtector _rootProtector;
	internal static readonly UTF8Encoding SecureUtf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public PersonalDataProtector(IDataProtectionProvider rootProtector)
	{
		_rootProtector = rootProtector.CreateProtector("Identity", "PersonalData") as IPersistedDataProtector ?? throw new NotSupportedException("Unable to constructed persisted data protector");
	}

	/// <inheritdoc />
	public string Protect(string data)
	{
		var bytes = SecureUtf8Encoding.GetBytes(data);
		var cipherText = _rootProtector.Protect(bytes);
		return Convert.ToBase64String(cipherText);
	}

	/// <inheritdoc />
	public string Unprotect(string data)
	{
		var dataBytes = Convert.FromBase64String(data);
		var plainBytes = _rootProtector.Unprotect(dataBytes);

		return SecureUtf8Encoding.GetString(plainBytes);
	}
}