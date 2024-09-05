using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class MediaEntryLogic
    {
        class NormHash
        {
            public NormHash() { }

            public NormHash(string norm, string hash, int order)
            {
                Norm = norm;
                Hash = hash;
                Order = order;
            }

            public string Norm { get; set; }
            public string Hash { get; set; }
            public int Order { get; set; }
        }


        private static List<string> FixList(List<string> lst)
        {
            if (lst == null)
                return new List<string>();

            return lst.Select(item => (item + string.Empty).NormalizeMiscCharacters().Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Substring(0, Math.Min(item.Length, Constants.MAX_NAME_LENGTH)))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Call FixList before passing to this method
        /// </summary>
        private static List<NormHash> CreateNormalizedList(List<string> lst, bool lowerCase)
        {
            var ret = new List<NormHash>();

            for (int i = 0; i < lst.Count; i++)
            {
                string norm = lst[i];
                if (lowerCase)
                    norm = norm.ToLower();

                string hash = Crypto.NormalizedHash(norm);
                if (!ret.Any(item => item.Hash == hash))
                    ret.Add(new NormHash(norm, hash, i));
            }

            return ret;
        }



        ///// <summary>
        ///// Save the mediaEntry BEFORE calling this method - it must have a valid id
        ///// </summary>
        //public static async Task UpdateSearchTerms(bool isNewEntry, MediaEntry mediaEntry, List<string> searchTerms)
        //{
        //    using var ctx = new AppDbContext();

        //    //Normalize
        //    var normLst = CreateNormalizedList(FixList(searchTerms), true);
        //    var hashes = normLst.Select(item => item.Hash).ToList();

        //    //Find existing terms based on hash
        //    var dbSearchTerms = hashes.Count > 0 ?
        //        await ctx.SearchTerms
        //            .AsNoTracking()
        //            .Where(item => hashes.Contains(item.Hash))
        //            .ToListAsync() :
        //        new List<SearchTerm>();

        //    //Add any new terms needed
        //    var newDBTerms = new List<SearchTerm>();
        //    foreach (var term in normLst)
        //        if (!dbSearchTerms.Any(item => item.Hash == term.Hash))
        //            if (!newDBTerms.Any(item => item.Hash == term.Hash))
        //                newDBTerms.Add(ctx.SearchTerms.Add(new SearchTerm { Term = term.Norm, Hash = term.Hash }).Entity);

        //    if (newDBTerms.Count > 0)
        //    {
        //        await ctx.SaveChangesAsync();
        //        foreach (var newDBTerm in newDBTerms)
        //            dbSearchTerms.Add(newDBTerm);
        //    }

        //    //Reset
        //    if (!isNewEntry)
        //    {
        //        var existingBridges = await ctx.MediaSearchBridges
        //            .AsNoTracking()
        //            .Where(item => item.MediaEntryId == mediaEntry.Id)
        //            .ToListAsync();

        //        if (existingBridges.Count > 0)
        //        {
        //            ctx.MediaSearchBridges.RemoveRange(existingBridges);
        //            await ctx.SaveChangesAsync();
        //        }
        //    }


        //    //Add Terms
        //    if (normLst.Count > 0)
        //    {
        //        ctx.ChangeTracker.Clear();
        //        foreach (var term in normLst)
        //        {
        //            var dbTerm = dbSearchTerms.First(item => item.Hash == term.Hash);
        //            ctx.MediaSearchBridges.Add(new MediaSearchBridge
        //            {
        //                MediaEntryId = mediaEntry.Id,
        //                SearchTermId = dbTerm.Id
        //            });
        //        }
        //        await ctx.SaveChangesAsync();
        //    }
        //}
    
    }
}