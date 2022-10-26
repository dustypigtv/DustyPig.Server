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

        

        public static async Task UpdateSearchTerms(AppDbContext DB, MediaEntry mediaEntry, List<string> searchTerms)
        {
            using var localCtx = new AppDbContext();

            //Normalize
            var normLst = CreateNormalizedList(FixList(searchTerms), true);
            var hashes = normLst.Select(item => item.Hash).ToList();

            //Find existing terms based on hash
            var dbSearchTerms = await localCtx.SearchTerms
                .AsNoTracking()
                .Where(item => hashes.Contains(item.Hash))
                .ToListAsync();

            //Add any new terms needed
            var newDBTerms = new List<SearchTerm>();
            foreach (var term in normLst)
                if (!dbSearchTerms.Any(item => item.Hash == term.Hash))
                    if(!newDBTerms.Any(item => item.Hash == term.Hash))
                        newDBTerms.Add(localCtx.SearchTerms.Add(new SearchTerm { Term = term.Norm, Hash = term.Hash }).Entity);

            if (newDBTerms.Count > 0)
            {
                await localCtx.SaveChangesAsync();
                foreach (var newDBTerm in newDBTerms)
                    dbSearchTerms.Add(newDBTerm);
            }


            //Update the media entry
            if (mediaEntry.MediaSearchBridges == null)
                mediaEntry.MediaSearchBridges = new List<MediaSearchBridge>();

            //Remove any terms that are not in the list
            foreach (var bridge in mediaEntry.MediaSearchBridges)
                if (!hashes.Contains(bridge.SearchTerm.Hash))
                    localCtx.MediaSearchBridges.Remove(bridge);

            //Add any new terms
            foreach (var term in normLst)
            {
                var dbTerm = dbSearchTerms.First(item => item.Hash == term.Hash);

                var exists = mediaEntry.MediaSearchBridges.Any(item => item.SearchTermId == dbTerm.Id);
                if (!exists)
                    mediaEntry.MediaSearchBridges.Add(new MediaSearchBridge
                    {
                        MediaEntry = mediaEntry,
                        SearchTermId = dbTerm.Id
                    });
            }
        }




        public static async Task UpdatePeople(AppDbContext DB, MediaEntry mediaEntry, List<string> cast, List<string> directors, List<string> producers, List<string> writers)
        {
            using var localCtx = new AppDbContext();

            cast = FixList(cast);
            directors = FixList(directors);
            producers = FixList(producers);
            writers = FixList(writers);

            var normLst = CreateNormalizedList(cast, false);
            normLst.AddRange(CreateNormalizedList(directors, false));
            normLst.AddRange(CreateNormalizedList(producers, false));
            normLst.AddRange(CreateNormalizedList(writers, false));

            var hashes = normLst.Select(item => item.Hash).Distinct().ToList();

            var dbPeople = await localCtx.People
                .AsNoTracking()
                .Where(item => hashes.Contains(item.Hash))
                .ToListAsync();


            //Add any new people needed
            var newDBPeople = new List<Person>();
            foreach (var person in normLst)
                if (!dbPeople.Any(item => item.Hash == person.Hash))
                    if(!newDBPeople.Any(item => item.Hash == person.Hash))
                        newDBPeople.Add(localCtx.People.Add(new Person { Name = person.Norm, Hash = person.Hash }).Entity);

            if (newDBPeople.Count > 0)
            {
                await localCtx.SaveChangesAsync();
                foreach (var newDBPerson in newDBPeople)
                    dbPeople.Add(newDBPerson);
            }


            //Update media entry
            if (mediaEntry.People == null)
                mediaEntry.People = new List<MediaPersonBridge>();


            foreach (var bridge in mediaEntry.People)
                if (!hashes.Contains(bridge.Person.Hash))
                    DB.MediaPersonBridges.Remove(bridge);

            //This fixes the cast sort problem
            //mediaEntry.People.RemoveAll(item => item.Role == Roles.Cast);
            foreach (var person in mediaEntry.People.Where(item => item.Role == Roles.Cast))
                DB.MediaPersonBridges.Remove(person);

            AddNewPeople(mediaEntry, cast, normLst, dbPeople, Roles.Cast);
            AddNewPeople(mediaEntry, directors, normLst, dbPeople, Roles.Director);
            AddNewPeople(mediaEntry, producers, normLst, dbPeople, Roles.Producer);
            AddNewPeople(mediaEntry, writers, normLst, dbPeople, Roles.Writer);

            //This fixes the context.update problem
            mediaEntry.People.ForEach(item => item.Person = null);
                        
        }

        private static void AddNewPeople(MediaEntry mediaEntry, List<string> people, List<NormHash> normLst, List<Person> dbPeople, Roles role)
        {
            int sort = 0;
            foreach (string person in people)
            {
                var normItem = normLst.First(item => item.Hash == Crypto.NormalizedHash(person));
                var exists = mediaEntry.People
                    .Where(item => item.Person.Hash == normItem.Hash)
                    .Where(item => item.Role == role)
                    .Any();

                if (!exists)
                    mediaEntry.People.Add(new MediaPersonBridge
                    {
                        MediaEntry = mediaEntry,
                        Person = dbPeople.First(item => item.Hash == normItem.Hash),
                        PersonId = dbPeople.First(item => item.Hash == normItem.Hash).Id,
                        Role = role,
                        SortOrder = sort++
                    });

            }
        }
    }
}