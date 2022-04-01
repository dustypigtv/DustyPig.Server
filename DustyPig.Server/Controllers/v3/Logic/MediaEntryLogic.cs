using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class MediaEntryLogic
    {
        public static async Task UpdateSearchTerms(AppDbContext DB, MediaEntry mediaEntry, List<string> searchTerms)
        {
            var dbSearchTerms = await DB.SearchTerms
                .AsNoTracking()
                .Where(item => searchTerms.Contains(item.Term))
                .ToListAsync();

            foreach (string term in searchTerms)
            {
                var exists = dbSearchTerms
                    .Where(item => item.Term == term)
                    .Any();

                if (!exists)
                    dbSearchTerms.Add(DB.SearchTerms.Add(new SearchTerm { Term = term }).Entity);
            }

            if (mediaEntry.MediaSearchBridges == null)
                mediaEntry.MediaSearchBridges = new List<MediaSearchBridge>();

            foreach (var bridge in mediaEntry.MediaSearchBridges)
                if (!searchTerms.Contains(bridge.SearchTerm.Term))
                    DB.MediaSearchBridges.Remove(bridge);

            foreach (string term in searchTerms)
            {
                var exists = mediaEntry.MediaSearchBridges
                    .Where(item => item.SearchTerm.Term == term)
                    .Any();

                if (!exists)
                    mediaEntry.MediaSearchBridges.Add(new MediaSearchBridge
                    {
                        MediaEntry = mediaEntry,
                        SearchTerm = dbSearchTerms.First(item => item.Term == term)
                    });
            }
        }


        public static async Task UpdatePeople(AppDbContext DB, MediaEntry mediaEntry, List<string> cast, List<string> directors, List<string> producers, List<string> writers)
        {
            if (cast == null) cast = new List<string>();
            if (directors == null) directors = new List<string>();
            if (producers == null) producers = new List<string>();
            if (writers == null) writers = new List<string>();

            var allPeople = new List<string>();
            allPeople.AddRange(cast);
            allPeople.AddRange(directors);
            allPeople.AddRange(producers);
            allPeople.AddRange(writers);
            allPeople = allPeople.Distinct().ToList();


            var dbPeople = await DB.People
                .AsNoTracking()
                .Where(item => allPeople.Contains(item.Name))
                .ToListAsync();

            foreach (string person in allPeople)
            {
                var exists = dbPeople
                    .Where(item => item.Name == person)
                    .Any();

                if (!exists)
                    dbPeople.Add(DB.People.Add(new Person { Name = person }).Entity);
            }


            if (mediaEntry.People == null)
                mediaEntry.People = new List<MediaPersonBridge>();

            foreach (var bridge in mediaEntry.People)
                if (!allPeople.Contains(bridge.Person.Name))
                    DB.MediaPersonBridges.Remove(bridge);

            AddNewPeople(mediaEntry, cast, dbPeople, Roles.Cast);
            AddNewPeople(mediaEntry, directors, dbPeople, Roles.Director);
            AddNewPeople(mediaEntry, producers, dbPeople, Roles.Producer);
            AddNewPeople(mediaEntry, writers, dbPeople, Roles.Writer);
        }

        private static void AddNewPeople(MediaEntry mediaEntry, List<string> people, List<Person> dbPeople, Roles role)
        {
            int sort = 0;
            foreach (string person in people)
            {
                var exists = mediaEntry.People
                       .Where(item => item.Person.Name == person)
                       .Where(item => item.Role == role)
                       .Any();

                if (!exists)
                    mediaEntry.People.Add(new MediaPersonBridge
                    {
                        Person = dbPeople.First(item => item.Name == person),
                        Role = role,
                        SortOrder = sort++
                    });

            }
        }
    }
}