using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Events.Data;
using Events.Web.Models.Follow;
using Microsoft.AspNet.Identity;

namespace Events.Web.Controllers
{
    [Authorize]
    public class FollowController : BaseController
    {
        //
        // GET: /Follow/
        public ActionResult Index()
        {
            List<FollowModel> model = new List<FollowModel>();

            model = GetFollowers();

            return View("FollowView", model);
        }

        public ActionResult FollowNewUser(string id)
        {
            Followship f = new Followship();
            f.Follower1 = this.User.Identity.GetUserId();
            f.Follower2 = id;
            eventsdb.Followships.Add(f);

            eventsdb.SaveChanges();
            
            return Redirect("~/Follow/Index");
        }

        public ActionResult Unsubscribe(string id)
        {
            string userId = this.User.Identity.GetUserId();

            Followship friendship = eventsdb.Followships.SingleOrDefault(f => f.Follower1.Equals(userId, StringComparison.InvariantCultureIgnoreCase)
                                          && f.Follower2.Equals(id, StringComparison.InvariantCultureIgnoreCase));

            eventsdb.Followships.Remove(friendship);
            eventsdb.SaveChanges();

            return Redirect("~/Follow/Index");
        }

        private List<FollowModel> GetFollowers()
        {
            string userId = this.User.Identity.GetUserId();

            List<AspNetUser> followList = eventsdb.Followships.Where(f => f.Follower1.Equals(userId, StringComparison.InvariantCultureIgnoreCase)).Select(f => f.AspNetUser1).ToList();

            return followList.Select(f1 => new FollowModel
                                    {
                                        Id = f1.Id,
                                        Email = f1.UserName,
                                        FullName = f1.FullName
                                    }).ToList();
        }
    }
}
