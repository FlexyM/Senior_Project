using System.Web.Mvc;

using Events.Data;

using Microsoft.AspNet.Identity;
using Events.External;
using System;
using System.Security.Claims;

namespace Events.Web.Controllers
{
    [ValidateInput(false)]
    public abstract class BaseController : Controller
    {
        protected ApplicationDbContext db = new ApplicationDbContext();
        protected EventsDataEntities eventsdb = new EventsDataEntities();
        protected EventfulEntities eventfulDb = new EventfulEntities();

        public bool IsAdmin()
        {
            var currentUserId = this.User.Identity.GetUserId();
            var isAdmin = (currentUserId != null && this.User.IsInRole("Administrator"));
            return isAdmin;
        }
    }
}
