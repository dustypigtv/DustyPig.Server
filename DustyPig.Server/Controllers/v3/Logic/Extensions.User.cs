using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static int? GetAccountId(this ClaimsPrincipal @this)
        {
            try { return int.Parse(@this.Claims.First(item => item.Type == JWTProvider.CLAIM_ACCOUNT_ID).Value); }
            catch { return null; }
        }

        public static int? GetProfileId(this ClaimsPrincipal @this)
        {
            try { return int.Parse(@this.Claims.First(item => item.Type == JWTProvider.CLAIM_PROFILE_ID).Value); }
            catch { return null; }
        }

        public static int? GetAuthTokenId(this ClaimsPrincipal @this)
        {
            try { return int.Parse(@this.Claims.First(item => item.Type == JWTProvider.CLAIM_AUTH_TOKEN_ID).Value); }
            catch { return null; }
        }

        public static int? GetFCMTokenId(this ClaimsPrincipal @this)
        {
            try { return int.Parse(@this.Claims.First(item => item.Type == JWTProvider.CLAIM_FCM_TOKEN_ID).Value); }
            catch { return null; }
        }


        public static async Task<(Account Account, Profile Profile)> VerifyAsync(this ClaimsPrincipal @this)
        {
            var acctId = @this.GetAccountId();
            var authTokenId = @this.GetAuthTokenId();
            var profId = @this.GetProfileId();
            var fcmTokenId = @this.GetFCMTokenId();

            if (acctId == null || authTokenId == null)
                return (null, null);

            using var db = new AppDbContext();

            var account = await db.Accounts
                .AsNoTracking()
                .Include(item => item.AccountTokens)
                .Include(item => item.Profiles)
                .ThenInclude(item => item.FCMTokens)
                .Where(item => item.Id == acctId.Value)
                .FirstOrDefaultAsync();

            if (account == null)
                return (null, null);

            if (!account.AccountTokens.Any(item => item.Id == authTokenId.Value))
                return (null, null);

            Profile profile = profId == null ? null : account.Profiles.FirstOrDefault(item => item.Id == profId.Value);

            if (profile != null && fcmTokenId != null)
            {
                var dbToken = profile.FCMTokens.FirstOrDefault(item => item.Id == fcmTokenId);

                //Only update once/day
                if (dbToken != null && dbToken.LastSeen.AddDays(1) < DateTime.UtcNow)
                {
                    db.Entry(dbToken).State = EntityState.Modified;
                    dbToken.LastSeen = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }

            return (account, profile);
        }
    }
}
