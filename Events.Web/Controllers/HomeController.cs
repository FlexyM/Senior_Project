using System;
using System.Linq;
using System.Web.Mvc;

using Events.Web.Models;
using Events.Web.Extensions;

using Microsoft.AspNet.Identity;
using Events.External;
using System.Web;
using System.Collections.Generic;

namespace Events.Web.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var events = this.eventsdb.Events
                .Where(e => e.IsPublic)
                .OrderBy(e => e.StartDateTime);

            var eventViews = EventsController.AddImagesToOurEvents(events);

            var upcomingEvents = eventViews.Where(e => e.StartDateTime > DateTime.Now).ToList();
            var passedEvents = eventViews.Where(e => e.StartDateTime <= DateTime.Now).ToList();

            var result = (new UpcomingPassedEventsViewModel()
            {
                UpcomingEvents = upcomingEvents,
                PassedEvents = passedEvents
            });

            //Getting results from Eventful
            EventfulSearch api = new EventfulSearch();
            api.Location = "Atlanta";
            api.Date = DateTime.Now.AddDays(1).ToString("yyyyMMdd") + "00-" + DateTime.Now.AddDays(15).ToString("yyyyMMdd") + "00";

            var eventfulEvents = api.Search().ConvertToEventViewModel();


            //Add the Eventful Events to the database events and resort       
            var temp = eventfulEvents.Where(e => e.StartDateTime > DateTime.Now).OrderBy(m => m.StartDateTime).AddMissingImagesToTheirEvents();
            result.UpcomingEvents = result.UpcomingEvents.Concat(temp).ToList();
            var temp1 = eventfulEvents.Where(e => e.StartDateTime <= DateTime.Now).OrderBy(m => m.StartDateTime).AddMissingImagesToTheirEvents();
            result.PassedEvents = result.PassedEvents.Concat(temp1).ToList(); 
            
            return View(result);
        }

        [HttpPost]
        public ActionResult Index(string location, string keyword, string fromDate, string toDate)
        {
            List<EventViewModel> events;
            //Local events search
            events = LocalEventSearch(location, keyword);
            
            //Local date search
            events = LocalEventsDateSearch(fromDate, toDate, events);

            //Local events sorting
            var upcomingEvents = events.Where(e => e.StartDateTime > DateTime.Now).ToList();
            var passedEvents = events.Where(e => e.StartDateTime <= DateTime.Now).ToList();

            var result = (new UpcomingPassedEventsViewModel()
            {
                UpcomingEvents = upcomingEvents,
                PassedEvents = passedEvents
            });

            //Getting results from Eventful
            EventfulSearch api = new EventfulSearch();
            if (!string.IsNullOrEmpty(keyword))
                api.Keyword = keyword;
            if (!string.IsNullOrEmpty(location))
                api.Location = location;

            api.Date = FormatEventfulSearchDateRange(fromDate, toDate);

            var eventfulEvents = api.Search().ConvertToEventViewModel();

            //Add the Eventful Events to the database events and resort       
            var temp = eventfulEvents.Where(e => e.StartDateTime > DateTime.Now).OrderBy(m => m.StartDateTime).AddMissingImagesToTheirEvents();
            result.UpcomingEvents = result.UpcomingEvents.Concat(temp).ToList();
            var temp1 = eventfulEvents.Where(e => e.StartDateTime <= DateTime.Now).OrderBy(m => m.StartDateTime).AddMissingImagesToTheirEvents();
            result.PassedEvents = result.PassedEvents.Concat(temp1).ToList(); 

            return View(result);
        }

        public ActionResult EventDetailsById(int id, string eventfulId, bool eventfulEvent)
        {
            if (!eventfulEvent)
            {
                var currentUserId = this.User.Identity.GetUserId();
                var isAdmin = this.IsAdmin();
                var eventDetails = this.eventsdb.Events
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


                if (r != null && r.Any())
                {
                    foreach (EventfulComment c in r)
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

        private List<EventViewModel> LocalEventSearch(string location, string keyword)
        {
            List<EventViewModel> events = new List<EventViewModel>();

            //Local events search
            if (!string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(keyword))
            {
                events = this.eventsdb.Events
                   .Where(e => e.IsPublic && e.Title.Contains(keyword) && e.City.Contains(location))
                   .OrderBy(e => e.StartDateTime)
                   .Select(EventViewModel.ViewModel).ToList();
            }
            else if (!string.IsNullOrEmpty(location))
            {
                events = events.Concat(this.eventsdb.Events
                 .Where(e => e.IsPublic && e.City.Contains(location))
                 .OrderBy(e => e.StartDateTime)
                 .Select(EventViewModel.ViewModel)).ToList();
            }
            else if (!string.IsNullOrEmpty(keyword))
            {
                events = events.Concat(this.eventsdb.Events
                 .Where(e => e.IsPublic && e.Title.Contains(keyword))
                 .OrderBy(e => e.StartDateTime)
                 .Select(EventViewModel.ViewModel)).ToList();
            }

            return events;
        }

        private List<EventViewModel> LocalEventsDateSearch(string fromDate, string toDate, List<EventViewModel> events)
        {
            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                return events.Where(e => e.StartDateTime >= DateTime.Parse(fromDate) && e.StartDateTime <= DateTime.Parse(toDate)).ToList();
            }
            else if (!string.IsNullOrEmpty(fromDate))
            {
                return events.Where(e => e.StartDateTime >= DateTime.Parse(fromDate)).ToList();
            }
            else if (!string.IsNullOrEmpty(toDate))
            {
                return events.Where(e => e.StartDateTime <= DateTime.Parse(toDate)).ToList();
            }
            else
                return events;
        }

        private string FormatEventfulSearchDateRange(string fromDate, string toDate)
        {
            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                return DateTime.Parse(fromDate).ToString("yyyyMMdd") + "00-" + DateTime.Parse(toDate).ToString("yyyyMMdd") + "00";
            }
            else if (!string.IsNullOrEmpty(fromDate))
            {
                return DateTime.Parse(fromDate).ToString("yyyyMMdd") + "00-"; // +DateTime.Parse(fromDate).AddDays(15).ToString("yyyyMMdd") + "00";
            }
            else if (!string.IsNullOrEmpty(toDate))
            {
                return DateTime.Now.AddDays(1).ToString("yyyyMMdd") + "00-" + DateTime.Parse(toDate).ToString("yyyyMMdd") + "00";
            }
            else
                return DateTime.Now.AddDays(1).ToString("yyyyMMdd") + "00-" + DateTime.Now.AddDays(15).ToString("yyyyMMdd") + "00";
        }
    }
}
