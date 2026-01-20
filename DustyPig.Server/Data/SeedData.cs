using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Firebase.Auth.Models;
using DustyPig.Server.Data.Models;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;

namespace DustyPig.Server.Data;

internal static class SeedData
{
    public static Account SeedAccount() => new Account
    {
        Id = TestAccount.AccountId,
        FirebaseId = TestAccount.FirebaseId
    };

    public static Profile SeedProfile() => new Profile
    {
        Id = TestAccount.ProfileId,
        AccountId = TestAccount.AccountId,
        AvatarUrl = TestAccount.AvatarUrl,
        IsMain = true,
        MaxMovieRating = MovieRatings.Unrated,
        MaxTVRating = TVRatings.NotRated,
        Name = TestAccount.Name
    };

    public static Library[] SeedLibraries() => new Library[]
    {
        new Library
        {
            Id = -1,
            AccountId = TestAccount.AccountId,
            Name = "Movies"
        },

        new Library
        {
            Id = -2,
            AccountId = TestAccount.AccountId,
            Name = "TV Shows",
            IsTV = true
        }
    };

    public static MediaEntry[] SeedMediaEntries()
    {
        var ret = new MediaEntry[]
        {
            new MediaEntry
            {
                Id = -1,
                LibraryId = -1,
                EntryType = MediaTypes.Movie,
                TMDB_Id = 10378,
                Title = "Big Buck Bunny",
                SortTitle = "big buck bunny",
                Date = new DateOnly(2008, 04, 10),
                Description = "Follow a day of the life of Big Buck Bunny when he meets three bullying rodents: Frank, Rinky, and Gamera. The rodents amuse themselves by harassing helpless creatures by throwing fruits, nuts and rocks at them. After the deaths of two of Bunny's favorite butterflies, and an offensive attack on Bunny himself, Bunny sets aside his gentle nature and orchestrates a complex plan for revenge.",
                Length = 596.458333,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.jpg",
                BackdropUrl = "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.backdrop.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.bif",
                Added = DateTime.UtcNow,
                MovieRating = MovieRatings.G,
                Genre_Animation = true,
                Genre_Comedy = true,
                Genre_Family = true
            },

            new MediaEntry
            {
                Id = -2,
                LibraryId = -1,
                EntryType = MediaTypes.Movie,
                TMDB_Id = 457784,
                Title = "Agent 327: Operation Barbershop",
                SortTitle = "agent 327 operation barbershop",
                Date = new DateOnly(2017, 05, 12),
                Description = "Agent 327 is investigating a clue that leads him to a shady barbershop in Amsterdam. Little does he know that he is being tailed by mercenary Boris Kloris.",
                Length = 231.458333,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.jpg",
                BackdropUrl = "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.backdrop.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.bif",
                Added = DateTime.UtcNow,
                MovieRating = MovieRatings.G,
                Genre_Action = true,
                Genre_Animation = true,
                Genre_Comedy = true
            },

            new MediaEntry
            {
                Id = -3,
                LibraryId = -1,
                EntryType = MediaTypes.Movie,
                TMDB_Id = 593048,
                Title = "Spring",
                SortTitle = "spring",
                Date = new DateOnly(2019, 04, 04),
                Description = "The story of a shepherd girl and her dog who face ancient spirits in order to continue the cycle of life.",
                Length = 464.083333,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.jpg",
                BackdropUrl = "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.backdrop.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.bif",
                Added = DateTime.UtcNow,
                MovieRating = MovieRatings.PG,
                Genre_Adventure = true,
                Genre_Animation = true,
                Genre_Fantasy = true
            },

            new MediaEntry
            {
                Id = -4,
                LibraryId = -1,
                EntryType = MediaTypes.Movie,
                Title = "Hero",
                SortTitle = "hero",
                Date = new DateOnly(2018, 04, 16),
                Description = "Hero is a showcase for the updated Grease Pencil tools in Blender 2.80. Grease Pencil means 2D animation tools within a full 3D pipeline.",
                Length = 236.653078,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.jpg",
                BackdropUrl = "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.backdrop.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.bif",
                Added = DateTime.UtcNow,
                MovieRating = MovieRatings.G,
                Genre_Animation = true,
                Genre_Fantasy = true
            },

            new MediaEntry
            {
                Id = -5,
                LibraryId = -1,
                EntryType = MediaTypes.Movie,
                TMDB_Id = 717986,
                Title = "Coffee Run",
                SortTitle = "coffee run",
                Date = new DateOnly(2020, 05, 29),
                Description = "Fueled by caffeine, a young woman runs through the bittersweet memories of her past relationship.",
                Length = 184.583333,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.jpg",
                BackdropUrl = "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.backdrop.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.bif",
                Added = DateTime.UtcNow,
                MovieRating = MovieRatings.G,
                Genre_Animation = true
            },

            new MediaEntry
            {
                Id = -6,
                LibraryId = -2,
                EntryType = MediaTypes.Series,
                Title = "Caminandes",
                SortTitle = "caminandes",
                Description = "The Caminandes cartoon series follows our hero Koro the Llama as he explores Patagonia, attempts to overcome various obstacles, and becomes friends with Oti the pesky penguin.",
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/show.jpg",
                BackdropUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/backdrop.jpg",
                Added = DateTime.UtcNow,
                TVRating = TVRatings.TV_G,
                Genre_Animation = true,
                Genre_Children = true,
                Genre_Family = true
            },

            new MediaEntry
            {
                Id = -7,
                LibraryId = -2,
                EntryType = MediaTypes.Episode,
                Title = "Llama Drama",
                Date = new DateOnly(2013, 09, 29),
                Description = "Koro has trouble crossing an apparent desolate road, a problem that an unwitting Armadillo does not share.",
                LinkedToId = 6,
                Season = 1,
                Episode = 1,
                Length = 90.001,
                CreditsStartTime = 87.917,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.bif",
                Added = DateTime.UtcNow
            },

            new MediaEntry
            {
                Id = -8,
                LibraryId = -2,
                EntryType = MediaTypes.Episode,
                Title = "Gran Dillama",
                Date = new(2013, 11, 22),
                Description = "Koro hunts for food on the other side of a fence and is once again inspired by the Armadillo but this time to a shocking effect.",
                LinkedToId = 6,
                Season = 1,
                Episode = 2,
                Length = 146.008,
                CreditsStartTime = 119.25,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.bif",
                Added = DateTime.UtcNow
            },

            new MediaEntry
            {
                Id = -9,
                LibraryId = -2,
                EntryType = MediaTypes.Episode,
                Title = "Llamigos",
                Date = new DateOnly(2013, 12, 20),
                Description = "Koro meets Oti, a pesky Magellanic penguin, in an epic battle over tasty red berries during the winter.",
                LinkedToId = 6,
                Season = 1,
                Episode = 3,
                Length = 150.048,
                CreditsStartTime = 139.5,
                ArtworkUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.jpg",
                VideoUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.mp4",
                BifUrl = "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.bif",
                Added = DateTime.UtcNow
            }
        };

        foreach(var mediaEntry in ret)
            mediaEntry.SetComputedInfo([], mediaEntry.GetGenreFlags(), null);

        return ret;
    }
}
