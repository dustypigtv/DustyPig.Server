using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    public abstract class _MediaControllerBase : _BaseProfileController
    {
        internal const int LIST_SIZE = 1000;

        //Put these in sort order
        internal const long ID_CONTINUE_WATCHING = DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING;
        internal const string ID_CONTINUE_WATCHING_TITLE = DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING_TITLE;

        internal const long ID_WATCHLIST = DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST;
        internal const string ID_WATCHLIST_TITLE = DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST_TITLE;


        internal const long ID_PLAYLISTS = DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS;
        internal const string ID_PLAYLISTS_TITLE = DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS_TITLE;

        
        internal const long ID_RECENTLY_ADDED = -2;
        internal const long ID_POPULAR = -1;

        internal readonly Services.TMDBClient _tmdbClient;


        internal _MediaControllerBase(AppDbContext db, Services.TMDBClient tmdbClient) : base(db)
        {
            _tmdbClient = tmdbClient;
        }


        internal async Task<ActionResult> DeleteMedia(int id)
        {
            //Get the object, making sure it's owned
            var mediaEntry = await DB.MediaEntries
                .Include(item => item.Library)
                .Include(item => item.Subtitles)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (mediaEntry == null || mediaEntry.Library.AccountId != UserAccount.Id)
                return NotFound("Either the specified item does not exist or is not owned by this account");

            DB.MediaEntries.Remove(mediaEntry);
            await DB.SaveChangesAsync();

            return Ok();
        }


        internal async Task UpdatePopularity(MediaEntry me)
        {
            if (!(me.EntryType == MediaTypes.Movie || me.EntryType == MediaTypes.Series))
                return;

            if (me.TMDB_Id == null)
                return;

            if (me.TMDB_Id <= 0)
                return;

            try
            {
                me.Popularity = await DB.MediaEntries
                    .Where(item => item.TMDB_Id == me.TMDB_Id)
                    .Where(item => item.EntryType == me.EntryType)
                    .Where(item => item.Popularity != null)
                    .Where(item => item.Popularity > 0)
                    .Select(item => item.Popularity)
                    .FirstOrDefaultAsync();

                if (me.Popularity == null)
                {
                    if (me.EntryType == MediaTypes.Movie)
                    {
                        var movie = await _tmdbClient.GetMovieAsync(me.TMDB_Id.Value);
                        me.Popularity = movie.Success ? movie.Data.Popularity : 0;
                    }
                    else //Series
                    {
                        var series = await _tmdbClient.GetSeriesAsync(me.TMDB_Id.Value);
                        me.Popularity = series.Success ? series.Data.Popularity : 0;
                    }
                }
            }
            catch
            {
                me.Popularity = 0;
            }
            me.PopularityUpdated = DateTime.UtcNow;
        }


        internal static List<string> GetSearchTerms(MediaEntry me, List<string> extraSearchTerms)
        {
            var ret = me.Title.NormalizedQueryString().Tokenize();

            //This handles variations like Spider-Man and Agents of S.H.I.E.L.D.
            var squished = me.Title.Replace("-", null).Replace(".", null).NormalizedQueryString().Tokenize();
            ret.AddRange(squished);

            //Add genres
            if (me.Genres.HasValue)
                ret.AddRange(me.Genres.Value.AsString().NormalizedQueryString().Tokenize());
            

            if (extraSearchTerms != null)
                ret.AddRange(extraSearchTerms.Select(item => (item + string.Empty).Trim().NormalizeMiscCharacters()));

            ret.RemoveAll(item => string.IsNullOrWhiteSpace(item));

            return ret.Distinct().ToList();
        }



        internal static IOrderedQueryable<MediaEntry> ApplySortOrder(IQueryable<MediaEntry> q, SortOrder sortOrder)
        {
            if (sortOrder == SortOrder.Alphabetical)
                return q.OrderBy(item => item.SortTitle);

            if (sortOrder == SortOrder.Alphabetical_Descending)
                return q.OrderByDescending(item => item.SortTitle);

            if (sortOrder == SortOrder.Added)
                return q.OrderBy(item => item.Added);

            if (sortOrder == SortOrder.Added_Descending)
                return q.OrderByDescending(item => item.Added);

            if (sortOrder == SortOrder.Released)
                return q.OrderBy(item => item.Date);

            if (sortOrder == SortOrder.Released_Descending)
                return q.OrderByDescending(item => item.Date);

            if (sortOrder == SortOrder.Popularity)
                return q.OrderBy(item => item.Popularity);

            if (sortOrder == SortOrder.Popularity_Descending)
                return q.OrderByDescending(item => item.Popularity);

            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }


    }
}
