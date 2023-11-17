using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    public abstract class _MediaControllerBase : _BaseProfileController
    {
        internal const int DEFAULT_LIST_SIZE = Constants.SERVER_RESULT_SIZE;
        internal const int ADMIN_LIST_SIZE = 100;
        internal const int MIN_GENRE_LIST_SIZE = 10;
        internal const int MAX_DB_LIST_SIZE = 1000; //This should be approximately # of Genres flags x DEFAULT_LIST_SIZE, which is currently 950

        internal readonly Services.TMDBClient _tmdbClient;


        internal _MediaControllerBase(AppDbContext db, Services.TMDBClient tmdbClient) : base(db)
        {
            _tmdbClient = tmdbClient;
        }


        internal async Task<ResponseWrapper> DeleteMedia(int id)
        {
            if (!UserProfile.IsMain)
                return CommonResponses.RequireMainProfile();


            //Get the object, making sure it's owned
            var mediaEntry = await DB.MediaEntries
                .Include(item => item.Library)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound(nameof(id));

            // Flag playlist artwork for updates
            var playlists = mediaEntry.EntryType == MediaTypes.Series ?
                await DB.MediaEntries
                    .Where(item => item.LinkedToId == id)
                    .Include(item => item.PlaylistItems)
                    .ThenInclude(item => item.Playlist)
                    .SelectMany(item => item.PlaylistItems)
                    .Select(item => item.Playlist)
                    .Distinct()
                    .ToListAsync() :

                await DB.PlaylistItems
                    .Where(item => item.MediaEntryId == id)
                    .Include(item => item.Playlist)
                    .Select(item => item.Playlist)
                    .Distinct()
                    .ToListAsync();

            playlists.ForEach(item => item.ArtworkUpdateNeeded = true);


            DB.MediaEntries.Remove(mediaEntry);
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
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
                    .Where(item => item.Id != me.Id)
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
            var ret = (me.Title + string.Empty).NormalizedQueryString().Tokenize();

            //This handles variations like Spider-Man and Agents of S.H.I.E.L.D.
            ret.AddRange
                (
                    (me.Title + string.Empty)
                        .Replace("-", null)
                        .Replace(".", null)
                        .NormalizedQueryString()
                        .Tokenize()
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item.Length > Constants.MAX_NAME_LENGTH ? item[..Constants.MAX_NAME_LENGTH] : item)
                        .Distinct()
                );

            //Add genres
            ret.AddRange(me.ToGenres().AsString().NormalizedQueryString().Tokenize());

            if (extraSearchTerms != null)
            {
                ret.AddRange
                    (
                        extraSearchTerms.SelectMany(item =>
                            (item + string.Empty)
                            .Trim()
                            .NormalizeMiscCharacters()
                            .Tokenize()
                            .Where(item2 => !string.IsNullOrWhiteSpace(item2))
                            .Select(item2 => item2.Length > Constants.MAX_NAME_LENGTH ? item[..Constants.MAX_NAME_LENGTH] : item)
                            .Distinct()
                    ));


                ret.AddRange
                    (
                        extraSearchTerms.SelectMany(item =>
                            (item + string.Empty)
                            .Trim()
                            .Replace("-", null)
                            .Replace(".", null)
                            .NormalizeMiscCharacters()
                            .Tokenize()
                            .Where(item2 => !string.IsNullOrWhiteSpace(item2))
                            .Select(item2 => item2.Length > Constants.MAX_NAME_LENGTH ? item[..Constants.MAX_NAME_LENGTH] : item)
                            .Distinct()
                    ));
            }

            return ret
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList();
        }



    }
}
