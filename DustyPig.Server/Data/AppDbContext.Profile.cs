using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext
    {
        public async Task<Account> GetOrCreateAccountAsync(string localId, string email)
        {
            var account = await this.Accounts
                .AsNoTracking()
                .Include(item => item.Profiles)
                .Where(item => item.FirebaseId == localId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                account = new Account { FirebaseId = localId };
                this.Accounts.Add(account);
                await this.SaveChangesAsync();
            }

            await this.GetOrCreateMainProfileAsync(account, email);

            return account;
        }


        public async Task<Profile> GetOrCreateMainProfileAsync(Account account, string email)
        {
            var acctProfiles = new List<Profile>();
            if (account.Profiles == null || account.Profiles.Count == 0)
            {
                var profiles = await this.Profiles
                    .AsNoTracking()
                    .Where(item => item.AccountId == account.Id)
                    .ToListAsync();
                acctProfiles.AddRange(profiles);
            }
            else
            {
                acctProfiles.AddRange(account.Profiles);
            }

            var mainProfile = acctProfiles.FirstOrDefault(item => item.IsMain);
            if (mainProfile == null)
            {
                mainProfile = this.Profiles.Add(new Profile
                {
                    AccountId = account.Id,
                    MaxMovieRating = MovieRatings.Unrated,
                    MaxTVRating = TVRatings.NotRated,
                    AvatarUrl = LogicUtils.EnsureProfilePic(null),
                    IsMain = true,
                    Name = email[..email.IndexOf("@")].Trim().ToLower(),
                    TitleRequestPermission = TitleRequestPermissions.Enabled
                }).Entity;

                int idx = 0;
                while (acctProfiles.Count(_ => _.Name.ICEquals(mainProfile.Name)) > 0)
                {
                    idx++;
                    mainProfile.Name = email[..email.IndexOf("@")].Trim().ToLower() + idx.ToString();
                }

                await SaveChangesAsync();
            }

            return mainProfile;
        }
    }
}
