using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Utilities
{
    public static class DeepLinks
    {
        public static string Create(Notification notification)
        {
            if (notification.MediaEntryId == null)
                return null;

            if(notification.NotificationType == NotificationType.OverrideRequest)
                return $"dustypig://overrides/{notification.TitleOverrideId}";

            if (notification.NotificationType == NotificationType.Media)
                return $"dustypig://media/{notification.MediaEntryId}";

            if (notification.NotificationType == NotificationType.Friend)
                return $"dustypig://friendship/{notification.FriendshipId}";

            if (notification.NotificationType == NotificationType.GetRequest)
                return $"dustypig://requests/{notification.GetRequestId}";

            return null;
        }
    }
}
