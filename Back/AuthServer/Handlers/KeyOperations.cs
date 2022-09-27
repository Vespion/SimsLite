using System.Security.Claims;
using System.Security.Cryptography;
using AuthServer.Data;
using FrontDTOs;
using FrontDTOs.Headers;
using FrontDTOs.Messages.Auth;
using FrontDTOs.Messages.Auth.Challenge;
using FrontDTOs.Messages.Auth.Register;
using FrontDTOs.Messages.Auth.Solve;
using FrontDTOs.StatusCodes;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerObjects;
using KeyChallenge = AuthServer.Data.KeyChallenge;

namespace AuthServer.Handlers
{
    [UsedImplicitly]
    public class KeyOperations :
        IHandle<RequestKeyChallenge>,
        IHandle<SolveKeyChallenge>,
        IHandle<KeyRegistration>
    {
        private readonly UserContext _context;
        private readonly UserManager<User> _userManager;

        public KeyOperations(UserContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public async Task<ResponseWrapper> Execute(RequestHeaders headers, RequestKeyChallenge body)
        {
            var device = await _context.UserDevices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == body.DeviceId);
            if (device == default)
            {
                return new ResponseWrapper
                {
                    Status = (int)ClientError.NotFound
                };
            }

            var challenge = new Memory<byte>(new byte[32]);
            RandomNumberGenerator.Fill(challenge.Span);

            var challengeId = Guid.NewGuid();
            _context.AuthenticationSessions.Add(new KeyChallenge
            {
                UserId = device.UserId,
                Challenge = Convert.ToBase64String(challenge.Span),
                Verifier = body.VeriferChallenge,
                Id = challengeId,
                DeviceId = device.Id
            });

            await _context.SaveChangesAsync();

            return new ResponseWrapper<FrontDTOs.Messages.Auth.Challenge.KeyChallenge>
            {
                Status = (int)Success.Ok,
                Data = new FrontDTOs.Messages.Auth.Challenge.KeyChallenge
                {
                    ChallengeId = challengeId,
                    Challenge = Convert.ToBase64String(challenge.Span)
                }
            };
        }

        /// <inheritdoc />
        public async Task<ResponseWrapper> Execute(RequestHeaders headers, SolveKeyChallenge body)
        {
            var session = await _context.AuthenticationSessions.OfType<KeyChallenge>().FirstOrDefaultAsync(x => x.Id == body.ChallengeId);
            if (session == null)
            {
                return new ResponseWrapper<CodeResponse>
                {
                    Status = (int)ClientError.NotFound,
                    Data = new CodeResponse
                    {
                        FailureReason = AuthFailures.Timeout
                    }
                };
            }

            var userKey = await _context.UserKeys.FirstAsync(x => x.DeviceId == session.DeviceId);

            using var rsaProvider = RSA.Create();
            rsaProvider.ImportRSAPublicKey(userKey.PublicKey, out _);

            var signatureValid = rsaProvider.VerifyData(Convert.FromBase64String(session.Challenge),
                Convert.FromBase64String(body.SignedChallenge), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (!signatureValid)
            {
                return new ResponseWrapper<CodeResponse>
                {
                    Status = (int)ClientError.AuthenticationMissing,
                    Data = new CodeResponse
                    {
                        FailureReason = AuthFailures.ChallengeFailed
                    }
                };
            }

            var authCodeBuffer = new byte[8];
            RandomNumberGenerator.Fill(authCodeBuffer);

            session.AuthCode = Convert.ToBase64String(authCodeBuffer);
            await _context.SaveChangesAsync();

            return new ResponseWrapper<CodeResponse>
            {
                Status = (int)Success.Ok,
                Data = new CodeResponse
                {
                    AuthCode = $"{session.Id:N}:{session.AuthCode}"
                }
            };
        }

        /// <inheritdoc />
        public async Task<ResponseWrapper> Execute(RequestHeaders headers, KeyRegistration body)
        {
            var user = await _userManager.FindByEmailAsync(body.UserEmail);
            if (user != null)
            {
                //TODO Verify it really is this user
            }

            var newUser = new User
            {
                Email = body.UserEmail,
                FirstName = body.User?.FirstName ?? "",
                LastName = body.User?.LastName ?? "",
                LockoutEnabled = true,
            };
            newUser.Keys = new List<UserKey>
        {
            new UserKey
            {
                Attestation = body.Attestation,
                Device = new Device
                {
                    DeviceName = body.DeviceName,
                    Id = body.DeviceId,
                    User = newUser
                },
                PublicKey = Convert.FromBase64String(body.PublicKey)
            }
        };
            var result = await _userManager.CreateAsync(newUser);

            if (result.Succeeded)
            {
                var cr = await _userManager.AddClaimAsync(newUser,
                    new Claim("SimsLite.Teacher", body.User!.IsTeacher.ToString(), ClaimValueTypes.Boolean, "Auth",
                        "Client"));
                if (cr.Succeeded)
                {
                    return new ResponseWrapper
                    {
                        Status = (int)Success.Ok
                    };
                }
            }

            if (result.Errors.Any(x => x.Code == "DuplicateEmail"))
            {
                return new ResponseWrapper
                {
                    Status = (int)ClientError.DuplicateEmail
                };
            }

            return new ResponseWrapper
            {
                Status = (int)ClientError.UserRegistrationError
            };
        }

        /// <inheritdoc />
        public Task<ResponseWrapper> Execute(RequestHeaders headers, object body)
        {
            return body switch
            {
                RequestKeyChallenge req => Execute(headers, req),
                SolveKeyChallenge req => Execute(headers, req),
                KeyRegistration req => Execute(headers, req),
                _ => throw new NotImplementedException()
            };
        }
    }
}