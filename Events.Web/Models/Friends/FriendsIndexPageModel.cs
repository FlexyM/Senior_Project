using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Events.Web.Models.Friends
{
    public class FriendsIndexPageModel
    {
        public List<FriendsModel> Friends { get; set; }
        public List<FriendsModel> PendingRequests { get; set; }
        public List<FriendsModel> SentRequests { get; set; }
    }
}