using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthServer.Data;
using FrontDTOs;
using FrontDTOs.Headers;
using FrontDTOs.Messages.Auth;
using FrontDTOs.Messages.Auth.Exchange;
using FrontDTOs.StatusCodes;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerBase;
using ServerObjects;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace AuthServer.Handlers
{
    [UsedImplicitly]
    public class TokenGeneration :
        IHandle<ExchangeCode>,
        IHandle<ExchangeRefreshToken>
    {
        private readonly UserContext _context;
        private readonly UserManager<User> _manager;
        private readonly SignInManager<User> _signInManager;
        private readonly IOptions<ServerSettings> _serverSettings;
        private readonly IOptions<JwtOptions> _jwtOptions;
        private readonly IDataProtector _dataProtector;

        public TokenGeneration(UserContext context, UserManager<User> manager, SignInManager<User> signInManager, IOptions<ServerSettings> serverSettings, IOptions<JwtOptions> jwtOptions, IDataProtectionProvider protectionProvider)
        {
            _context = context;
            _manager = manager;
            _signInManager = signInManager;
            _serverSettings = serverSettings;
            _jwtOptions = jwtOptions;
            _dataProtector = protectionProvider.CreateProtector("Tokens");
        }

        /// <inheritdoc />
        public async Task<ResponseWrapper> Execute(RequestHeaders headers, ExchangeCode body)
        {
            var splitCode = body.AuthorizationCode.Split(':');
            var sessionId = splitCode[0];
            var authCode = splitCode[1];

            var session = await _context.AuthenticationSessions.FindAsync(sessionId);

            if (session == null)
            {
                return new ResponseWrapper
                {
                    Status = (int)ClientError.NotFound
                };
            }

            if (session.AuthCode != authCode)
            {
                return new ResponseWrapper
                {
                    Status = (int)ClientError.ValidationError
                };
            }

            var hashedVerifier = SHA256.HashData(Convert.FromBase64String(body.Verifer));
            if (session.Verifier != Convert.ToBase64String(hashedVerifier))
            {
                //The auth code matches but the verifier the client originally supplied does not
                return new ResponseWrapper
                {
                    Status = (int)ClientError.Unauthorized
                };
            }

            var (accessToken, refreshToken) = await GenerateTokensForUser(session.UserId);
            var protector = _dataProtector.CreateProtector("Refresh");
            
            return new ResponseWrapper<TokenResponse>
            {
                Status = (int)Success.Ok,
                Data = new TokenResponse
                {
                    AccessToken = accessToken.ToString(),
                    RefreshToken = protector.Protect(refreshToken.ToString())
                }
            };
        }

        private async Task<(JwtSecurityToken access, JwtSecurityToken refresh)> GenerateTokensForUser(Guid userId)
        {
            var user = await _manager.FindByIdAsync(userId.ToString());
            var userPrincipal = await _signInManager.CreateUserPrincipalAsync(user);

            using var rsaProvider = RSA.Create();
            rsaProvider.ImportRSAPrivateKey(Convert.FromBase64String(_jwtOptions.Value.Key!), out _);

            var signingKey = new RsaSecurityKey(rsaProvider);
            var signingCredential = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var builder = new JwtSecurityTokenHandler();

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _serverSettings.Value.Issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = _serverSettings.Value.Audience,
                Subject = (ClaimsIdentity)userPrincipal.Identity!,
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.Value.Expiry ?? 10),
                NotBefore = DateTime.UtcNow,
                SigningCredentials = signingCredential,
                AdditionalHeaderClaims =
                {
                    {JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")}
                },
                Claims =
                {
                    {ClaimTypes.Name, user.FirstName},
                    {ClaimTypes.Surname, user.LastName},
                }
            };

            var token = builder.CreateJwtSecurityToken(descriptor);

            var signedToken = token.ToString();

            user.Tokens.Add(new Token
            {
                Id = token.Id,
                Refresh = false,
                SecurityToken = signedToken,
                UserId = userId
            });

            var refreshDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _serverSettings.Value.Issuer,
                Audience = _serverSettings.Value.Audience,
                IssuedAt = DateTime.UtcNow, 
                Expires = DateTime.UtcNow.AddDays(7),
                Claims =
                {
                    {JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")},
                    { JwtRegisteredClaimNames.Sub, user.Id.ToString("N") },
                    { ClaimTypes.Role, "refresh" }
                },
                SigningCredentials = signingCredential
            };

            var refreshToken = builder.CreateJwtSecurityToken(refreshDescriptor);

            user.Tokens.Add(new Token
            {
                Id = refreshToken.Id,
                Refresh = true,
                SecurityToken = refreshToken.ToString(),
                UserId = userId
            });

            await _manager.UpdateAsync(user);

            return (token, refreshToken);
        }

        /// <inheritdoc />
        public async Task<ResponseWrapper> Execute(RequestHeaders headers, ExchangeRefreshToken body)
        {
            var protector = _dataProtector.CreateProtector("Refresh");
            var decryptedToken = protector.Unprotect(body.RefreshToken);

            using var rsaProvider = RSA.Create();
            rsaProvider.ImportRSAPrivateKey(Convert.FromBase64String(_jwtOptions.Value.Key!), out _);

            var signingKey = new RsaSecurityKey(rsaProvider);

            var handler = new JwtSecurityTokenHandler();
            var result = await handler.ValidateTokenAsync(decryptedToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidIssuer = _serverSettings.Value.Issuer,
                ValidAudience = _serverSettings.Value.Audience
            });

            if (!result.IsValid)
            {
                return new ResponseWrapper
                {
                    Status = (int)ClientError.AuthenticationExpired
                };
            }
            
            var token = await _context.Tokens.FindAsync(result.SecurityToken.Id);
            if (token?.Refresh != true)
            {
                return new ResponseWrapper<TokenResponse>
                {
                    Status = (int)ClientError.AuthenticationMissing
                };
            }

            var (accessToken, newRefreshToken) = await GenerateTokensForUser(token.UserId);

            return new ResponseWrapper<TokenResponse>
            {
                Status = (int)Success.Ok,
                Data = new TokenResponse
                {
                    AccessToken = accessToken.ToString(),
                    RefreshToken = protector.Protect(newRefreshToken.ToString())
                }
            };
        }

        /// <inheritdoc />
        public Task<ResponseWrapper> Execute(RequestHeaders headers, object body)
        {
            return body switch
            {
                ExchangeRefreshToken req => Execute(headers, req),
                ExchangeCode req => Execute(headers, req),
                _ => throw new NotImplementedException()
            };
        }
    }
}