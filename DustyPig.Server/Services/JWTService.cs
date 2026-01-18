using DustyPig.Server.Data;
using DustyPig.Server.Extensions;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DustyPig.Server.Services;

internal class JWTService : IDisposable
{
    private const string CONFIG_KEY = "JWT-KEY";


    public const string ISSUER = "dustypig.tv";
    public const string AUDIENCE = "dusty-pig-clients";
    public const string CLAIM_ACCOUNT_ID = "account_id";
    public const string CLAIM_PROFILE_ID = "profile_id";
    public const string CLAIM_AUTH_TOKEN_ID = "auth_token_id";
    public const string CLAIM_FCM_TOKEN_ID = "fcm_token_id";
        

    private readonly AppDbContext _db;
    private readonly SigningCredentials _signingCredentials;

    private bool _disposed;
    
   
    public JWTService(IConfiguration configuration, AppDbContext db)
    {
        _db = db;
        var signingKey = GetSecurityKey(configuration);
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512Signature);
    }

    public static SymmetricSecurityKey GetSecurityKey(IConfiguration configuration) =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetRequiredValue(CONFIG_KEY)));


    public async Task<string> CreateTokenAsync(int accountId, int? profileId, int? fcmTokenId, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = null;
        }
        else
        {
            deviceId = Crypto.HashString(deviceId);

            var toDel = await _db.AccountTokens
                .Where(a => a.AccountId == accountId)
                .Where(a => a.DeviceId == deviceId)
                .ToListAsync();
            _db.AccountTokens.RemoveRange(toDel);
        }

        var acctToken = _db.AccountTokens.Add(new Data.Models.AccountToken
        {
            AccountId = accountId,
            DeviceId = deviceId
        }).Entity;
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _db.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~JWTProvider()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}