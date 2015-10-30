namespace Events.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Mvc;

    using Events.Web.Models;
    using Events.Web.Extensions;

    using Microsoft.AspNet.Identity;
    using Events.External;
    using System.Web;
    using System.Collections.Generic;


    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var events = this.db.Events
                .OrderBy(e => e.StartDateTime)
                .Where(e => e.IsPublic)
                .Select(EventViewModel.ViewModel);

            var upcomingEvents = events.Where(e => e.StartDateTime > DateTime.Now).ToList();
            var passedEvents = events.Where(e => e.StartDateTime <= DateTime.Now).ToList();

            var result = (new UpcomingPassedEventsViewModel()
            {
                UpcomingEvents = upcomingEvents,
                PassedEvents = passedEvents
            });

            //Getting results from Eventful
            EventfulSearch api = new EventfulSearch();
            api.City = "Atlanta"; //TODO get real value from user input
            var eventfulEvents = api.Search().ConvertToEventViewModel();

            //Add the Eventful Events to the database events and resort       

            result.UpcomingEvents = result.UpcomingEvents.Concat(eventfulEvents.Where(e => e.StartDateTime > DateTime.Now)).OrderBy(m => m.StartDateTime);
            result.PassedEvents = result.PassedEvents.Concat(eventfulEvents.Where(e => e.StartDateTime <= DateTime.Now)).OrderBy(m => m.StartDateTime);

            return View(result);
        }

        public ActionResult EventDetailsById(int id, string eventfulId, bool eventfulEvent)
        {
            if (!eventfulEvent)
            {
                var currentUserId = this.User.Identity.GetUserId();
                var isAdmin = this.IsAdmin();
                var eventDetails = this.db.Events
                    .Where(e => e.Id == id)
                    .Where(e => e.IsPublic || isAdmin || (e.AuthorId != null && e.AuthorId == currentUserId))
                    .Select(EventDetailsViewModel.ViewModel)
                    .FirstOrDefault();

                var isOwner = (eventDetails != null && eventDetails.AuthorId != null &&
                    eventDetails.AuthorId == currentUserId);
                this.ViewBag.CanEdit = isOwner || isAdmin;

                return this.PartialView("_EventDetails", eventDetails);
            }
            else
            {
                this.ViewBag.CanEdit = false;
                EventDetailsViewModel result = new EventDetailsViewModel();

                EventfulSearch search = new EventfulSearch();
                search.Id = eventfulId;
                var eventResult = search.GetEventfulDetails();

                if (eventResult.description != null)
                    result.Description = HttpUtility.HtmlDecode(eventResult.description);
                else
                    result.Description = "No additional details.";

                result.Id = eventfulId;

                List<CommentViewModel> comments = new List<CommentViewModel>();
                var r = this.eventfulDb.EventfulComments.Where(c => c.EventfulId == eventfulId).ToList();


                if (r!= null && r.Any())
                {
                    foreach(EventfulComment c in r)
                    {
                        CommentViewModel cView = new CommentViewModel();

                        cView.Text = c.Text;
                        cView.Author = c.AspNetUser.FullName;

                        comments.Add(cView);
                    }
                }

                result.Comments = comments;

                return this.PartialView("_EventDetails", result);
            }
        }
    }
}
