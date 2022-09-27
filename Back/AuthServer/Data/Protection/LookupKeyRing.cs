using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Protection;

public class LookupKeyRing : ILookupProtectorKeyRing
{
	private readonly IKeyManager _keyManager;

	public LookupKeyRing(IKeyManager keyManager)
	{
		_keyManager = keyManager;
		CurrentKeyId = GetAllKeyIds().First();
	}

	/// <inheritdoc />
	public IEnumerable<string> GetAllKeyIds()
	{
		return _keyManager.GetAllKeys().OrderBy(x => x.ExpirationDate).Where(x => x.IsRevoked == false).Select(x => x.KeyId.ToString());
	}

	/// <inheritdoc />
	public string CurrentKeyId { get; }

	/// <inheritdoc />
	public string this[string keyId] => throw new NotImplementedException();
}