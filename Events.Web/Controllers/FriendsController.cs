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
        //
        // GET: /Friends/
        public ActionResult Index()
        {
            string userid = this.User.Identity.GetUserId();
            var friendships = eventsdb.Friendships.Where(f => f.Friend1.Equals(userid,StringComparison.InvariantCultureIgnoreCase)
                                                           || f.Friend2.Equals(userid,StringComparison.InvariantCultureIgnoreCase)).ToList();

            List<AspNetUser> friends = friendships.Where(f => f.Friend1 != userid).Select(f => f.AspNetUser).ToList();
            friends.AddRange(friendships.Where(f => f.Friend2 != userid).Select(f => f.AspNetUser1).ToList());


            return View("FriendsView", friends.Select(f => new FriendsModel 
                                { 
                                 FriendshipId = f.Id,
                                 Email = f.Email,
                                 FullName = f.FullName
                                }));
        }

        //
        // GET: /Friends/Create
        public ActionResult AddFriend(string emailAddress)
        {
            string userid = this.User.Identity.GetUserId();
            
            var friendships = eventsdb.Friendships.Where(f => f.Friend1.Equals(userid,StringComparison.InvariantCultureIgnoreCase)
                                                           || f.Friend2.Equals(userid,StringComparison.InvariantCultureIgnoreCase)).ToList();

            List<AspNetUser> friends = friendships.Where(f => f.Friend1 != userid).Select(f => f.AspNetUser).ToList();
            friends.AddRange(friendships.Where(f => f.Friend2 != userid).Select(f => f.AspNetUser1).ToList());

            var query = from users in eventsdb.AspNetUsers
                        where (users.Email.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase)
                            || users.FullName.Contains(emailAddress))
                        select users;

            var enemies = query.Select(q => q.Id).Except(friends.Select(f => f.Id));

            //TODO Select the AspNetUsers from the query variable matching the GuidIDs witht the list in enemies. Build a FriendsView List and display on a new View (AddFriendView)

            return View();
        }

        //
        // POST: /Friends/Create
        [HttpPost]
        public ActionResult AddFriend(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // POST: /Friends/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
