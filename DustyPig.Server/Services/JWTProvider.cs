using DustyPig.Server.Data;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DustyPig.Server.Services
{
    public class JWTProvider
    {
        public const string ISSUER = "dustypig.tv";
        public const string AUDIENCE = "dusty-pig-clients";
        public const string CLAIM_ACCOUNT_ID = "account_id";
        public const string CLAIM_PROFILE_ID = "profile_id";
        public const string CLAIM_AUTH_TOKEN_ID = "token_id";
        public const string CLAIM_FCM_TOKEN_ID = "device_token_id";

        public static SymmetricSecurityKey SigningKey { get; private set; }
        private static SigningCredentials _signingCredentials = null;

        private readonly AppDbContext _db;

        public static void Configure(string signingKey)
        {
            SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            _signingCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha512Signature);
        }

        public JWTProvider(AppDbContext db) => _db = db;

        public async Task<string> CreateTokenAsync(int accountId, int? profileId, int? fcmTokenId)
        {
            var acctToken = _db.AccountTokens.Add(new Data.Models.AccountToken { AccountId = accountId }).Entity;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>();

            claims.Add(new Claim(CLAIM_AUTH_TOKEN_ID, acctToken.Id.ToString()));
            claims.Add(new Claim(CLAIM_ACCOUNT_ID, accountId.ToString()));
            if (profileId != null)
            {
                claims.Add(new Claim(CLAIM_PROFILE_ID, profileId.Value.ToString()));

                if (fcmTokenId != null)
                    claims.Add(new Claim(CLAIM_FCM_TOKEN_ID, fcmTokenId.Value.ToString()));
            }

            var token = new JwtSecurityToken(ISSUER, AUDIENCE, claims, null, DateTime.UtcNow.AddYears(10), _signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}