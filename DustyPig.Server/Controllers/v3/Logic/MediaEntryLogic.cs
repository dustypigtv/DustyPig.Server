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

            lst = lst.Select(item => (item + string.Empty).NormalizeMiscCharacters().Trim()).Distinct().ToList();
            lst.RemoveAll(item => string.IsNullOrWhiteSpace(item));
            lst = lst.Select(item => item.Substring(0, Math.Min(item.Length, Constants.MAX_NAME_LENGTH))).Distinct().ToList();

            return lst;
        }

        /// <summary>
        /// Call FixList before passing to this method
        /// </summary>
        private static List<NormHash> CreateNormalizedList(List<string> lst, bool lowerCase)
        {
            var ret = new List<NormHash>();

            for(int i = 0; i < lst.Count; i++)
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



        /// <summary>
        /// Save the mediaEntry BEFORE calling this method - it must have a valid id
        /// </summary>
        public static async Task UpdateSearchTerms(bool isNewEntry, MediaEntry mediaEntry, List<string> searchTerms)
        {
            using var ctx = new AppDbContext();

            //Normalize
            var normLst = CreateNormalizedList(FixList(searchTerms), true);
            var hashes = normLst.Select(item => item.Hash).ToList();

            //Find existing terms based on hash
            var dbSearchTerms = hashes.Count > 0 ?
                await ctx.SearchTerms
                .AsNoTracking()
                .Where(item => hashes.Contains(item.Hash))
                .ToListAsync() :
                new List<SearchTerm>();

            //Add any new terms needed
            var newDBTerms = new List<SearchTerm>();
            foreach (var term in normLst)
                if (!dbSearchTerms.Any(item => item.Hash == term.Hash))
                    if (!newDBTerms.Any(item => item.Hash == term.Hash))
                        newDBTerms.Add(ctx.SearchTerms.Add(new SearchTerm { Term = term.Norm, Hash = term.Hash }).Entity);

            if (newDBTerms.Count > 0)
            {
                await ctx.SaveChangesAsync();
                foreach (var newDBTerm in newDBTerms)
                    dbSearchTerms.Add(newDBTerm);
            }

            //Reset
            if (!isNewEntry)
            {
                var existingBridges = await ctx.MediaSearchBridges
                    .AsNoTracking()
                    .Where(item => item.MediaEntryId == mediaEntry.Id)
                    .ToListAsync();

                if (existingBridges.Count > 0)
                {
                    ctx.MediaSearchBridges.RemoveRange(existingBridges);
                    await ctx.SaveChangesAsync();
                }
            }


            //Add Terms
            if (normLst.Count > 0)
            {
                foreach (var term in normLst)
                {
                    var dbTerm = dbSearchTerms.First(item => item.Hash == term.Hash);
                    ctx.MediaSearchBridges.Add(new MediaSearchBridge
                    {
                        MediaEntryId = mediaEntry.Id,
                        SearchTermId = dbTerm.Id
                    });
                }
                await ctx.SaveChangesAsync();
            }
        }


        /// <summary>
        /// Save the mediaEntry BEFORE calling this method - it must have a valid id
        /// </summary>
        public static async Task UpdatePeople(bool isNewEntry, MediaEntry mediaEntry, List<string> cast, List<string> directors, List<string> producers, List<string> writers)
        {
            using var ctx = new AppDbContext();

            cast = FixList(cast);
            directors = FixList(directors);
            producers = FixList(producers);
            writers = FixList(writers);

            var normLst = CreateNormalizedList(cast, false);
            normLst.AddRange(CreateNormalizedList(directors, false));
            normLst.AddRange(CreateNormalizedList(producers, false));
            normLst.AddRange(CreateNormalizedList(writers, false));

            var hashes = normLst.Select(item => item.Hash).Distinct().ToList();

            var dbPeople = hashes.Count > 0 ?
                await ctx.People
                .AsNoTracking()
                .Where(item => hashes.Contains(item.Hash))
                .ToListAsync() :
                new List<Person>();


            //Add any new people needed
            var newDBPeople = new List<Person>();
            foreach (var person in normLst)
                if (!dbPeople.Any(item => item.Hash == person.Hash))
                    if(!newDBPeople.Any(item => item.Hash == person.Hash))
                        newDBPeople.Add(ctx.People.Add(new Person { Name = person.Norm, Hash = person.Hash }).Entity);

            if (newDBPeople.Count > 0)
            {
                await ctx.SaveChangesAsync();
                foreach (var newDBPerson in newDBPeople)
                    dbPeople.Add(newDBPerson);
            }


            //Reset
            if (!isNewEntry)
            {
                var existingBridges = await ctx.MediaPersonBridges
                    .AsNoTracking()
                    .Where(item => item.MediaEntryId == mediaEntry.Id)
                    .ToListAsync();

                if (existingBridges.Count > 0)
                {
                    ctx.MediaPersonBridges.RemoveRange(existingBridges);
                    await ctx.SaveChangesAsync();
                }
            }


            if (cast.Count + directors.Count + producers.Count + writers.Count == 0)
                return;

            //Add bridges
            ctx.ChangeTracker.Clear();
            AddNewPeople(ctx, mediaEntry.Id, cast, normLst, dbPeople, Roles.Cast);
            AddNewPeople(ctx, mediaEntry.Id, directors, normLst, dbPeople, Roles.Director);
            AddNewPeople(ctx, mediaEntry.Id, producers, normLst, dbPeople, Roles.Producer);
            AddNewPeople(ctx, mediaEntry.Id, writers, normLst, dbPeople, Roles.Writer);

            //This fixes the context.update problem
            await ctx.SaveChangesAsync();                        
        }

        private static void AddNewPeople(AppDbContext context, int mediaEntryId, List<string> people, List<NormHash> normLst, List<Person> dbPeople, Roles role)
        {
            int sort = 0;
            foreach (string person in people)
            {
                var normItem = normLst.First(item => item.Hash == Crypto.NormalizedHash(person));
                var bridge = new MediaPersonBridge
                {
                    MediaEntryId = mediaEntryId,
                    PersonId = dbPeople.First(item => item.Hash == normItem.Hash).Id,
                    Role = role,
                    SortOrder = sort++
                };
                context.MediaPersonBridges.Add(bridge);
            }
        }

    }
}