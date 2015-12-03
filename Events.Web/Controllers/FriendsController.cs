using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Events.Web.Models.Friends;
using Events.Data;

namespace Events.Web.Controllers
{
    [Authorize]
    public class FriendsController : BaseController
    {
        public ActionResult Index()
        {
            FriendsIndexPageModel model = new FriendsIndexPageModel();
            string userid = this.User.Identity.GetUserId();
            
            //Get Friendships
            var friendships = eventsdb.Friendships.Where(f => f.Friend1.Equals(userid,StringComparison.InvariantCultureIgnoreCase)
                                                           || f.Friend2.Equals(userid,StringComparison.InvariantCultureIgnoreCase)).ToList();

            //Get Friends
            List<AspNetUser> friends = friendships.Where(f => f.Friend1 != userid).Select(f => f.AspNetUser).ToList();
            friends.AddRange(friendships.Where(f => f.Friend2 != userid).Select(f => f.AspNetUser1).ToList());

            //Get pending friends requests
            List<AspNetUser> pendingRequests = eventsdb.FriendRequests.Where(fr => fr.ToUser.Equals(userid, StringComparison.InvariantCultureIgnoreCase)
                                                                                && !fr.Approved
                                                                                && !fr.Declined).Select(req => req.AspNetUser1).ToList();

            //Get sent friend requests
            List<AspNetUser> sentRequests = eventsdb.FriendRequests.Where(fr => fr.FromUser.Equals(userid, StringComparison.InvariantCultureIgnoreCase)
                                                                                && !fr.Approved
                                                                                && !fr.Declined).Select(req => req.AspNetUser).ToList();

            //Add friends to model
            model.Friends = friends.Select(f => new FriendsModel 
                                            { 
                                                Id = f.Id,
                                                Email = f.Email,
                                                FullName = f.FullName
                                            }).ToList();

            //Add pending requests to model
            model.PendingRequests = pendingRequests.Select(f => new FriendsModel
                                                        {
                                                            Id = f.Id,
                                                            Email = f.Email,
                                                            FullName = f.FullName
                                                        }).ToList();

            //Add sent requests to model
            model.SentRequests = sentRequests.Select(f => new FriendsModel
                                                    {
                                                        Id = f.Id,
                                                        Email = f.Email,
                                                        FullName = f.FullName
                                                    }).ToList();

            return View("FriendsView", model);
        }

        public ActionResult AddFriend(string emailAddress)
        {
            string userid = this.User.Identity.GetUserId();
            
            var friendships = eventsdb.Friendships.Where(f => f.Friend1.Equals(userid,StringComparison.InvariantCultureIgnoreCase)
                                                           || f.Friend2.Equals(userid,StringComparison.InvariantCultureIgnoreCase)).ToList();

            List<AspNetUser> friends = friendships.Where(f => f.Friend1 != userid).Select(f => f.AspNetUser).ToList();
            friends.AddRange(friendships.Where(f => f.Friend2 != userid).Select(f => f.AspNetUser1).ToList());

            var searchUsers = from users in eventsdb.AspNetUsers
                        where (users.Email.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase)
                            || users.FullName.Contains(emailAddress))
                        select users;

            List<string> nonFriendsIds = searchUsers.Select(q => q.Id).Except(friends.Select(f => f.Id)).ToList();

            List<AspNetUser> nonFriends = searchUsers.Where(u => nonFriendsIds.Contains(u.Id)).ToList();

            return View("NonFriendsView", nonFriends.Select(f => new FriendsModel
            {
                Id = f.Id,
                Email = f.Email,
                FullName = f.FullName
            }));
        }

        public ActionResult SendFrindRequest(string id)
        {
            var currentUser = this.User.Identity.GetUserId();

            bool existingFriendship = eventsdb.FriendRequests.Any(fr => (fr.FromUser.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                                                              && fr.ToUser.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase) && fr.Declined == false && fr.Approved == false)
                                                        || (fr.FromUser.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase)
                                                              && fr.ToUser.Equals(id, StringComparison.InvariantCultureIgnoreCase) && fr.Declined == false && fr.Approved == false));
            if (!existingFriendship)
            {
                eventsdb.FriendRequests.Add(new FriendRequest()
                {
                    FromUser = currentUser,
                    ToUser = id
                });

                eventsdb.SaveChanges();
            }

            return Redirect("~/Friends/Index");
        }

        public ActionResult AcceptFriend(string id)
        {
            var currentUser = this.User.Identity.GetUserId();

            var friendRequest = eventsdb.FriendRequests.Where(fr => (fr.FromUser.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                                                              && fr.ToUser.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase))
                                                        || (fr.FromUser.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase)
                                                              && fr.ToUser.Equals(id, StringComparison.InvariantCultureIgnoreCase))).ToList();

            foreach (var request in friendRequest)
            {
                request.Approved = true;
                request.Declined = false;
            }

            eventsdb.Friendships.Add(new Friendship() { Friend1 = currentUser, Friend2 = id });

            eventsdb.SaveChanges();

            return Redirect("~/Friends/Index");
        }

        public ActionResult DeclineFriend(string id)
        {
            var currentUser = this.User.Identity.GetUserId();

            var friendRequest = eventsdb.FriendRequests.Where(fr => (fr.FromUser.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                                                              && fr.ToUser.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase))
                                                        || (fr.FromUser.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase)
                                                              && fr.ToUser.Equals(id, StringComparison.InvariantCultureIgnoreCase))).ToList();

            foreach (var request in friendRequest)
            {
                request.Approved = false;
                request.Declined = true;
            }

            eventsdb.SaveChanges();

            return Redirect("~/Friends/Index");
        }


        public ActionResult Delete(string id)
        {
                string currentUser = this.User.Identity.GetUserId();

                var friendships = eventsdb.Friendships.Where(fr => (fr.Friend1.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                                                  && fr.Friend2.Equals(currentUser,StringComparison.InvariantCultureIgnoreCase))
                                                  || (fr.Friend1.Equals(currentUser, StringComparison.InvariantCultureIgnoreCase)
                                                  && fr.Friend2.Equals(id,StringComparison.InvariantCultureIgnoreCase)));
                eventsdb.Friendships.RemoveRange(friendships);
                eventsdb.SaveChanges();

                return Redirect("~/Friends/Index");
        }
    }
}