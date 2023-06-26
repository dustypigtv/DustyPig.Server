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
        public static int? GetAccountId(this ClaimsPrincipal self)
        {
            try { return int.Parse(self.Claims.First(item => item.Type == JWTProvider.CLAIM_ACCOUNT_ID).Value); }
            catch { return null; }
        }

        public static int? GetProfileId(this ClaimsPrincipal self)
        {
            try { return int.Parse(self.Claims.First(item => item.Type == JWTProvider.CLAIM_PROFILE_ID).Value); }
            catch { return null; }
        }

        public static int? GetAuthTokenId(this ClaimsPrincipal self)
        {
            try { return int.Parse(self.Claims.First(item => item.Type == JWTProvider.CLAIM_AUTH_TOKEN_ID).Value); }
            catch { return null; }
        }

        public static int? GetFCMTokenId(this ClaimsPrincipal self)
        {
            try { return int.Parse(self.Claims.First(item => item.Type == JWTProvider.CLAIM_FCM_TOKEN_ID).Value); }
            catch { return null; }
        }


        public static async Task<(Account Account, Profile Profile)> VerifyAsync(this ClaimsPrincipal self)
        {
            var acctId = self.GetAccountId();
            var authTokenId = self.GetAuthTokenId();
            var profId = self.GetProfileId();
            var fcmTokenId = self.GetFCMTokenId();

            if (acctId == null || authTokenId == null)
                return (null, null);

            using var db = new AppDbContext();

            Account account;
            if (profId.HasValue)
            {
                account = await db.Accounts
                    .AsNoTracking()
                    .Include(a => a.AccountTokens.Where(t => t.Id == authTokenId))
                    .Include(a => a.Profiles.Where(p => p.Id == profId))
                    .ThenInclude(p => p.FCMTokens)
                    .Where(a => a.Id == acctId.Value)
                    .FirstOrDefaultAsync();
            }
            else
            {
                account = await db.Accounts
                    .AsNoTracking()
                    .Include(a => a.AccountTokens.Where(t => t.Id == authTokenId))
                    .Where(a => a.Id == acctId.Value)
                    .FirstOrDefaultAsync();
            }


            if (account == null)
                return (null, null);

            if (!account.AccountTokens.Any(item => item.Id == authTokenId.Value))
                return (null, null);

            Profile profile = account.Profiles?.FirstOrDefault(item => item.Id == profId.Value);
            if (profile != null && fcmTokenId != null)
            {
                var dbToken = profile.FCMTokens?.FirstOrDefault(item => item.Id == fcmTokenId);

                //Only update once/day
                if (dbToken?.LastSeen.AddDays(1) < DateTime.UtcNow)
                {
                    dbToken.LastSeen = DateTime.UtcNow;
                    db.FCMTokens.Update(dbToken);
                    await db.SaveChangesAsync();
                }
            }

            return (account, profile);
        }
    }
}
