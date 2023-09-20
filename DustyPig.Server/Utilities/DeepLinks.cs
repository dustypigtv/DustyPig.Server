using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Utilities
{
    public static class DeepLinks
    {
        public static string Create(Notification notification)
        {
            if (notification.NotificationType == NotificationType.OverrideRequest)
                return $"overrides/{notification.TitleOverrideId}";

            if (notification.NotificationType == NotificationType.Friend)
                return $"friendship/{notification.FriendshipId}";

            if (notification.NotificationType == NotificationType.GetRequest)
                return $"requests/{notification.GetRequestId}";

            if (notification.NotificationType == NotificationType.Media && notification.MediaEntry != null)
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
