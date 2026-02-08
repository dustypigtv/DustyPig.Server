using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using DustyPig.Server.Services.TMDB_Service;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using APIPerson = DustyPig.API.v3.Models.BasicPerson;

namespace DustyPig.Server.Extensions;

internal static class DataExtensions
{
    public static BasicLibrary ToBasicLibraryInfo(this FriendLibraryShare self, int accountId) => new BasicLibrary
    {
        Name = Misc.Coalesce(self.LibraryDisplayName, self.Library.Name),
        Id = self.LibraryId,
        IsTV = self.Library.IsTV,
        Owner = self.Friendship.GetFriendDisplayNameForAccount(accountId)
    };


    /// <summary>
    /// Make sure Friendship was loaded with:
    /// 
    ///     .Include(item => item.Account1)
    ///     .ThenInclude(item => item.Profiles)
    ///     
    ///     .Include(item => item.Account2)
    ///     .ThenInclude(item => item.Profiles)
    /// </summary>
    public static BasicFriend ToBasicFriendInfo(this Friendship self, int accountId)
    {
        var displayName = self.GetFriendDisplayNameForAccount(accountId);
        return new BasicFriend
        {
            Id = self.Id,
            DisplayName = displayName,
            Initials = displayName.GetInitials(),
            AvatarUrl = self.GetFriendAvatar(accountId),
            Accepted = self.Accepted,
            FriendRequestDirection = self.Account1Id == accountId ? RequestDirection.Sent : RequestDirection.Received
        };
    }



    /// <summary>
    /// Make sure friend was called with:
    ///     .Include(item => item.Account1)
    ///     .ThenInclude(item => item.Profiles)
    ///     .Include(item => item.Account2)
    ///     .ThenInclude(item => item.Profiles)
    /// </summary>
    public static string GetFriendDisplayNameForAccount(this Friendship friend, int accountId)
    {
        string displayName = friend.Account1Id == accountId ? friend.DisplayName2 : friend.DisplayName1;
        string acctName = friend.Account1Id == accountId
            ? friend.Account2.Profiles.Where(item => item.IsMain).First().Name
            : friend.Account1.Profiles.Where(item => item.IsMain).First().Name;

        return Misc.Coalesce(displayName, acctName);
    }

    /// <summary>
    /// Make sure friend was called with:
    ///     .Include(item => item.Account1)
    ///     .ThenInclude(item => item.Profiles)
    ///     .Include(item => item.Account2)
    ///     .ThenInclude(item => item.Profiles)
    /// </summary>
    public static string GetFriendAvatar(this Friendship friend, int accountId)
    {
        return friend.Account1Id == accountId
            ? friend.Account2.Profiles.Where(item => item.IsMain).First().AvatarUrl
            : friend.Account1.Profiles.Where(item => item.IsMain).First().AvatarUrl;
    }

    public static BasicLibrary ToBasicLibraryInfo(this Library self) => new BasicLibrary
    {
        Id = self.Id,
        IsTV = self.IsTV,
        Name = self.Name
    };

    /// <summary>
    /// Make sure Library was loaded with:
    /// 
    ///     .Include(item => item.Account)
    ///     .ThenInclude(item => item.Profiles)
    ///     
    ///     .Include(item => item.FriendLibraryShares)
    ///     .ThenInclude(item => item.FriendShip)
    ///     .ThenInclude(item => item.Account1)
    ///     .ThenInclude(item => item.Profiles)
    ///     
    ///     .Include(item => item.FriendLibraryShares)
    ///     .ThenInclude(item => item.FriendShip)
    ///     .ThenInclude(item => item.Account2)
    ///     .ThenInclude(item => item.Profiles)
    /// </summary>
    public static DetailedLibrary ToDetailedLibraryInfo(this Library self)
    {
        //This acct owns the lib
        var ret = new DetailedLibrary
        {
            Id = self.Id,
            IsTV = self.IsTV,
            Name = self.Name
        };

        foreach (var share in self.ProfileLibraryShares)
            if (self.Account.Profiles.Select(item => item.Id).Contains(share.ProfileId))
            {
                ret.Profiles ??= new();
                ret.Profiles.Add(share.Profile.ToBasicProfileInfo());
            }

        foreach (var friendship in self.FriendLibraryShares.Select(item => item.Friendship))
        {
            ret.SharedWith ??= new();
            ret.SharedWith.Add(friendship.ToBasicFriendInfo(self.AccountId));
        }

        return ret;
    }


    public static IOrderedQueryable<MediaEntry> ApplySortOrder(this IQueryable<MediaEntry> self, SortOrder sortOrder)
    {
        //Popularity DESC is the default

        return sortOrder switch
        {
            SortOrder.Added => self.OrderBy(item => item.Added),
            SortOrder.Added_Descending => self.OrderByDescending(item => item.Added),
            SortOrder.Alphabetical => self.OrderBy(item => item.SortTitle),
            SortOrder.Alphabetical_Descending => self.OrderByDescending(item => item.SortTitle),
            SortOrder.Popularity => self.OrderBy(item => item.Popularity),
            SortOrder.Released => self.OrderBy(item => item.Date),
            SortOrder.Released_Descending => self.OrderByDescending(item => item.Date),
            _ => self.OrderByDescending(item => item.Popularity)
        };
    }


    public static BasicMedia ToBasicMedia(this MediaEntry self, bool includeDescription = false)
    {
        var ret = new BasicMedia
        {
            Id = self.Id,
            ArtworkUrl = self.ArtworkUrl,
            BackdropUrl = self.BackdropUrl,
            MediaType = self.EntryType,
            Title = self.FormattedTitle()
        };

        if (includeDescription)
            ret.Description = self.Description;

        return ret;
    }


    public static DetailedMovie ToDetailedMovie(this MediaEntry self, bool playable)
    {
        //Build the response
        return new DetailedMovie
        {
            Added = self.Added,
            ArtworkUrl = self.ArtworkUrl,
            BackdropUrl = self.BackdropUrl,
            BifUrl = playable ? self.BifUrl : null,
            Credits = self.GetPeople(),
            CreditsStartTime = self.CreditsStartTime,
            Date = self.Date.Value,
            Description = self.Description,
            Genres = self.GetGenreFlags(),
            Id = self.Id,
            IntroEndTime = self.IntroEndTime,
            IntroStartTime = self.IntroStartTime,
            Length = self.Length.Value,
            LibraryId = self.LibraryId,
            CanPlay = playable,
            Rated = self.MovieRating ?? MovieRatings.None,
            Title = self.Title,
            TMDB_Id = self.TMDB_Id,
            VideoUrl = playable ? self.VideoUrl : null,
            ExtraSearchTerms = self.ExtraSearchTerms ?? []
        };
    }


    public static DetailedSeries ToAdminDetailedSeries(this MediaEntry self)
    {
        return new DetailedSeries
        {
            Added = self.Added,
            ArtworkUrl = self.ArtworkUrl,
            BackdropUrl = self.BackdropUrl,
            Credits = self.GetPeople(),
            Description = self.Description,
            Genres = self.GetGenreFlags(),
            Id = self.Id,
            LibraryId = self.LibraryId,
            Rated = self.TVRating ?? TVRatings.None,
            Title = self.Title,
            TMDB_Id = self.TMDB_Id,
            ExtraSearchTerms = self.ExtraSearchTerms ?? [],
            CanManage = true
        };
    }

    public static DetailedEpisode ToAdminDetailedEpisode(this MediaEntry self)
    {
        var ret = new DetailedEpisode
        {
            Added = self.Added,
            ArtworkUrl = self.ArtworkUrl,
            BifUrl = self.BifUrl,
            CreditsStartTime = self.CreditsStartTime,
            Date = self.Date.Value,
            Description = self.Description,
            EpisodeNumber = (ushort)self.Episode.Value,
            Id = self.Id,
            IntroEndTime = self.IntroEndTime,
            IntroStartTime = self.IntroStartTime,
            Length = self.Length.Value,
            SeasonNumber = (ushort)self.Season.Value,
            SeriesId = self.LinkedToId.Value,
            Title = self.Title,
            TMDB_Id = self.TMDB_Id,
            VideoUrl = self.VideoUrl,
        };

        return ret;
    }

    public static List<DetailedEpisode> ToAdminDetailedEpisodeList(this IEnumerable<MediaEntry> self)
    {
        var ret = new List<DetailedEpisode>();

        foreach (var ep in self)
            ret.Add(ep.ToAdminDetailedEpisode());

        return ret;
    }



    public static List<APIPerson> GetPeople(this MediaEntry self)
    {
        if (self?.TMDB_Entry?.People == null)
            return null;

        if (self.TMDB_Entry.People.Count == 0)
            return null;

        List<APIPerson> ret = null;

        foreach (CreditRoles role in Enum.GetValues(typeof(CreditRoles)))
        {
            var bridges = self.TMDB_Entry.People
                .Where(item => item.Role == role)
                .OrderBy(item => item.SortOrder)
                .ToList();

            foreach (var bridge in bridges)
            {
                ret ??= [];

                string avatarUrl = bridge.TMDB_Person.AvatarUrl;
                if (string.IsNullOrWhiteSpace(avatarUrl))
                    avatarUrl = Constants.DEFAULT_PROFILE_IMAGE_GREY;
                ret.Add(new APIPerson
                {
                    AvatarUrl = avatarUrl,
                    Initials = bridge.TMDB_Person.Name.GetInitials(),
                    Name = bridge.TMDB_Person.Name,
                    Order = bridge.SortOrder,
                    Role = role,
                    TMDB_Id = bridge.TMDB_PersonId
                });
            }
        }

        return ret;
    }


    //public static void SortSearchResults(this List<MediaEntry> self, string normQuery)
    //{
    //    var qt = new Dictionary<int, string>();
    //    foreach (var me in self)
    //        qt.Add(me.Id, me.Title.NormalizedQueryString());

    //    self.Sort((x, y) =>
    //    {
    //        int ret = -qt[x.Id].ICEquals(normQuery).CompareTo(qt[y.Id].ICEquals(normQuery));

    //        if (ret == 0 && qt[x.Id].ICEquals(qt[y.Id]))
    //            ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);

    //        if (ret == 0)
    //            ret = -qt[x.Id].ICStartsWith(normQuery).CompareTo(qt[y.Id].ICStartsWith(normQuery));

    //        if (ret == 0)
    //            ret = -qt[x.Id].ICContains(normQuery).CompareTo(qt[y.Id].ICContains(normQuery));

    //        if (ret == 0)
    //            ret = x.SortTitle.CompareTo(y.SortTitle);

    //        if (ret == 0)
    //            ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);

    //        return ret;
    //    });

    //}

    public static BasicMedia ToBasicMedia(this Data.Models.Playlist self)
    {
        var ret = new BasicMedia
        {
            Id = self.Id,
            MediaType = MediaTypes.Playlist,
            Title = self.Name,
            ArtworkUrl = self.ArtworkUrl,
            BackdropUrl = self.BackdropUrl,
        };


        return ret;
    }



    public static BasicProfile ToBasicProfileInfo(this Profile self) => new BasicProfile
    {
        Id = self.Id,
        Name = self.Name,
        Initials = self.Name.GetInitials(),
        AvatarUrl = self.AvatarUrl,
        HasPin = self.PinNumber != null && self.PinNumber >= 1000,
        IsMain = self.IsMain
    };


    public static BasicTMDB ToBasicTMDBInfo(this TMDB.Models.Search.MultiObject self) => new BasicTMDB
    {
        TMDB_ID = self.Id,
        ArtworkUrl = TMDBService.GetPosterPath(self.PosterPath),
        BackdropUrl = TMDBService.GetBackdropPath(self.BackdropPath),
        MediaType = self.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
        Title = self.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? self.Title : self.Name
    };

    public static API.v3.Models.BasicPerson ToTMDBPerson(this TMDB.Models.Search.MultiObject self) => new BasicPerson
    {
        TMDB_Id = self.Id,
        Name = self.Name,
        Initials = self.Name.GetInitials(),
        AvatarUrl = TMDBService.GetAvatarPath(self.ProfilePath)
    };


    public static int? GetAccountId(this ClaimsPrincipal self)
    {
        try { return int.Parse(self.Claims.First(item => item.Type == JWTService.CLAIM_ACCOUNT_ID).Value); }
        catch { return null; }
    }

    public static int? GetProfileId(this ClaimsPrincipal self)
    {
        try { return int.Parse(self.Claims.First(item => item.Type == JWTService.CLAIM_PROFILE_ID).Value); }
        catch { return null; }
    }

    public static int? GetAuthTokenId(this ClaimsPrincipal self)
    {
        try { return int.Parse(self.Claims.First(item => item.Type == JWTService.CLAIM_AUTH_TOKEN_ID).Value); }
        catch { return null; }
    }

    public static int? GetFCMTokenId(this ClaimsPrincipal self)
    {
        try { return int.Parse(self.Claims.First(item => item.Type == JWTService.CLAIM_FCM_TOKEN_ID).Value); }
        catch { return null; }
    }


    public static async Task<(Account Account, Profile Profile)> VerifyAsync(this ClaimsPrincipal self, AppDbContext db)
    {
        var acctId = self.GetAccountId();
        var authTokenId = self.GetAuthTokenId();
        var profId = self.GetProfileId();
        var fcmTokenId = self.GetFCMTokenId();

        if (acctId == null || authTokenId == null)
            return (null, null);

        var account = await db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountTokens.Where(t => t.Id == authTokenId))
            .Include(a => a.Profiles)
            .ThenInclude(item => item.FCMTokens)
            .Where(a => a.Id == acctId.Value)
            .FirstOrDefaultAsync();


        if (account == null)
            return (null, null);

        if (!account.AccountTokens.Any(item => item.Id == authTokenId.Value))
            return (null, null);

        if (!profId.HasValue)
            return (account, null);

        Profile profile = account.Profiles.FirstOrDefault(item => item.Id == profId.Value);
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