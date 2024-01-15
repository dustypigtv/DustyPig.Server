using DustyPig.Server.Data.Models;
using System.Linq;

namespace DustyPig.Server.Utilities
{
    public static class DeepLinks
    {
        public static string Create(Notification notification)
        {

            if (notification.NotificationType == NotificationType.Friend)
                return $"friendship/{notification.FriendshipId}";


            var mediaTypes = new NotificationType[]
{
                NotificationType.Media,
                NotificationType.OverrideRequest
            };

            if (notification.NotificationType == NotificationType.GetRequest)
            {
                if (mediaTypes.Contains(notification.NotificationType) && notification.MediaEntry != null)
                {
                    switch (notification.MediaEntry.EntryType)
                    {
                        case API.v3.Models.MediaTypes.Movie:
                            return $"requests/movie/{notification.MediaEntryId}";
                        case API.v3.Models.MediaTypes.Series:
                            return $"requests/series/{notification.MediaEntryId}";
                        default:
                            return null;
                    }
                }
            }


            
            if (mediaTypes.Contains(notification.NotificationType) && notification.MediaEntry != null)
            {
                switch (notification.MediaEntry.EntryType)
                {
                    case API.v3.Models.MediaTypes.Movie:
                        return $"movie/{notification.MediaEntryId}";
                    case API.v3.Models.MediaTypes.Series:
                        return $"series/{notification.MediaEntryId}";
                    default:
                        return null;
                }
            }

            return null;
        }
    }
}
