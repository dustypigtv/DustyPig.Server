using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext
    {
        /// <summary>
        /// Creates an Account and the main Profile
        /// </summary>
        public async Task<Account> CreateAccountAndProfileAsync(int id, string name, MovieRatings maxMovieRatings, TVRatings maxTVRatings, API.v3.Models.TitleRequestPermissions titleRequestPermissions, string avatarUrl, ushort? pinNumber)
        {
            var account = await Accounts
                .Include(item => item.Profiles)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (account == null)
            {
                account = Accounts.Add(new Account { Id = id }).Entity;
                await SaveChangesAsync();
            }

            var profile = await CreateOrUpdateProfileAsync(account, name, maxMovieRatings, maxTVRatings, titleRequestPermissions, avatarUrl, pinNumber, false);
            profile.IsMain = true;
            await SaveChangesAsync();

            return account;
        }

        public async Task<Profile> CreateOrUpdateProfileAsync(Account account, string name, MovieRatings maxMovieRatings, TVRatings maxTVRatings, API.v3.Models.TitleRequestPermissions titleRequestPermissions, string avatarUrl, ushort? pinNumber, bool locked)
        {
            name = name.Trim();

            var profile = account.Profiles.FirstOrDefault(item => item.Name.ICEquals(name));
            if (profile == null)
            {
                profile = new Profile
                {
                    Account = account,
                    AccountId = account.Id,
                    MaxMovieRating = maxMovieRatings,
                    MaxTVRating = maxTVRatings,
                    AvatarUrl = avatarUrl,
                    Locked = locked,
                    Name = name,
                    PinNumber = pinNumber,
                    TitleRequestPermission = titleRequestPermissions
                };

                account.Profiles.Add(profile);
            }
            else
            {
                profile.MaxMovieRating = maxMovieRatings;
                profile.MaxTVRating = maxTVRatings;
                profile.AvatarUrl = avatarUrl;
                profile.Locked = !profile.IsMain && locked;
                profile.Name = name;
                profile.PinNumber = pinNumber;
                profile.TitleRequestPermission = titleRequestPermissions;
            }

            await SaveChangesAsync();

            return profile;
        }
    }
}
