using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ApiSdk.Exceptions;
using FrontDTOs.Messages.Auth;
using FrontDTOs.Messages.Auth.Challenge;
using FrontDTOs.Messages.Auth.Exchange;
using FrontDTOs.Messages.Auth.Register;
using FrontDTOs.Messages.Auth.Solve;
using FrontDTOs.StatusCodes;

namespace ApiSdk
{
    public class SimAuthenticationManager
    {
        private SimApiClient _client;

        internal void Init(SimApiClient client)
        {
            _client = client;
        }

        public string SecurityToken { get; private set; }
        internal string RefreshToken { get; private set; }

        public async Task RegisterPublicKey(string deviceId, string deviceName, string firstName, string lastName, string userEmail, string publicKey, string attestation)
        {
            await _client.Socket.SendAsync(new MessageBuilder()
                .SetMessagePayload(new KeyRegistration
                {
                    DeviceId = deviceId,
                    UserEmail = userEmail,
                    PublicKey = publicKey,
                    Attestation = attestation,
                    DeviceName = deviceName,
                    User = new UserInfo
                    {
                        FirstName = firstName,
                        LastName = lastName
                    }
                })
                .Build());

            await Helpers.HandleResponse(_client.Socket, (i, wrapper) => {
                switch (i)
                {
                    case (int) ClientError.DuplicateEmail:
                        throw new DuplicateEmailException(wrapper, userEmail);
                    default:
                        throw new RegistrationException(wrapper);
                }
                });
        }

        public async Task SignInWithPublicKey(string deviceId, string userEmail, Func<string, ValueTask<string>> singingHandler)
        {
            var verifierSpan = new byte[8];
            RandomNumberGenerator.Create().GetBytes(verifierSpan);
            var verifier = Convert.ToBase64String(verifierSpan);
            var verifierChallenge = Convert.ToBase64String(SHA256.Create().ComputeHash(verifierSpan));

            await _client.Socket.SendAsync(new MessageBuilder()
                .SetMessagePayload(new RequestKeyChallenge
                {
                    DeviceId = deviceId,
                    UserEmail = userEmail,
                    VeriferChallenge = verifierChallenge
                })
                .Build());


            var challengeResponse = await Helpers.HandleResponse<KeyChallenge>(_client.Socket, (status, wrapper) =>
            {
                switch (status)
                {
                    case (int) ClientError.NotFound:
                        throw new ResourceNotFoundException(wrapper, deviceId, "UserKey_Device");
                    default:
                        throw new ApiException(wrapper);
                }
            });

            var signedChallenge = await singingHandler.Invoke(challengeResponse.Challenge);

            await _client.Socket.SendAsync(new MessageBuilder()
                .SetMessagePayload(new SolveKeyChallenge
                {
                    ChallengeId = challengeResponse.ChallengeId,
                    SignedChallenge = signedChallenge
                })
                .Build());

            var solveResponse = await Helpers.HandleResponse<CodeResponse>(_client.Socket);

            await _client.Socket.SendAsync(new MessageBuilder()
                .SetMessagePayload(new ExchangeCode
                {
                    AuthorizationCode = solveResponse.AuthCode,
                    Verifer = verifier
                })
                .Build());
            var tokenResponse = await Helpers.HandleResponse<TokenResponse>(_client.Socket);

            SecurityToken = tokenResponse?.AccessToken;
            RefreshToken = tokenResponse?.RefreshToken;
        }

        public async Task SignInWithRefreshToken()
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                return;
            }
            
            await _client.Socket.SendAsync(new MessageBuilder()
                .SetMessagePayload(new ExchangeRefreshToken
                {
                    RefreshToken = RefreshToken
                })
                .Build());
            
            var tokenResponse = await Helpers.HandleResponse<TokenResponse>(_client.Socket);

            SecurityToken = tokenResponse?.AccessToken;
            RefreshToken = tokenResponse?.RefreshToken;
        }
    }
}