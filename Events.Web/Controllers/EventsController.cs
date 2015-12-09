using System.Web.Mvc;
using System;
using System.Linq;

using Events.Data;
using Events.Web.Extensions;
using Events.Web.Models;

using Microsoft.AspNet.Identity;
using Events.External;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Web.Helpers;
using System.Net.Http.Headers;

namespace Events.Web.Controllers
{
    public class EventsController : BaseController
    {
        [Authorize]
        public ActionResult My()
        {
            string currentUserId = this.User.Identity.GetUserId();

            var events = this.eventsdb.Events
                .Where(e => e.AuthorId == currentUserId)
                .OrderBy(e => e.StartDateTime);

            var eventViews = AddImagesToOurEvents(events);

            var upcomingEvents = eventViews.Where(e => e.StartDateTime > DateTime.Now).ToList();
            var passedEvents = eventViews.Where(e => e.StartDateTime <= DateTime.Now).ToList();
            return View(new UpcomingPassedEventsViewModel()
            {
                UpcomingEvents = upcomingEvents,
                PassedEvents = passedEvents
            });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.States = GetStates();
            ViewBag.Friends = GetFriends();
            return this.View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventInputModel model)
        {
            string lat = string.Empty;
            string lon = string.Empty;
            EventfulSearch coorResolver = new EventfulSearch();
            string[] coord = resolveCoordinates(model.Address, model.City, model.State, model.Zip);

            int zip = 0;
            int.TryParse(model.Zip, out zip);

            if (coord != null && coord.Length == 2)
            {
                lat = coord[0];
                lon = coord[1];
            }

            if (model != null && this.ModelState.IsValid)
            {
                var e = new Event()
                {
                    AuthorId = this.User.Identity.GetUserId(),
                    Title = model.Title,
                    StartDateTime = model.StartDateTime,
                    Duration = model.Duration,
                    Description = model.Description,
                    Address = model.Address,
                    City = model.City,
                    Lat = lat,
                    Lon = lon,
                    State = model.State,
                    Zip = zip > 9999 ? new Nullable<int>(zip) : null,
                    IsPublic = model.IsPublic
                };

                this.eventsdb.Events.Add(e);
                this.eventsdb.SaveChanges();

                List<string> invitedUsers = new List<string>();
                if(Request["Friends"]!= null)
                {
                    invitedUsers = Request["Friends"].Split(',').ToList();
                }

                foreach(string invitedUser in invitedUsers)
                {
                    var invite = new EventInvite();
                    invite.FromUser = this.User.Identity.GetUserId();
                    invite.ToUser = invitedUser;
                    invite.EventId = e.Id;

                    eventsdb.EventInvites.Add(invite);
                }
                eventsdb.SaveChanges();

                if (Request.Files != null && Request.Files.Count > 0)
                {
                    SaveImages(e.Id);
                }

                this.AddNotification("Event created.", NotificationType.INFO);
                return this.RedirectToAction("My");
            }

            return this.View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            ViewBag.States = GetStates();
            ViewBag.Friends = GetFriends();

            var eventToEdit = this.LoadEvent(id);
            if (eventToEdit == null)
            {
                this.AddNotification("Cannot edit event #" + id, NotificationType.ERROR);
                return this.RedirectToAction("My");
            }

            var model = EventInputModel.CreateFromEvent(eventToEdit);
            return this.View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, EventInputModel model)
        {
            var eventToEdit = this.LoadEvent(id);
            if (eventToEdit == null)
            {
                this.AddNotification("Cannot edit event #" + id, NotificationType.ERROR);
                return this.RedirectToAction("My");
            }

            int zip = 0;
            int.TryParse(model.Zip, out zip);

            if (model != null && this.ModelState.IsValid)
            {
                eventToEdit.Title = model.Title;
                eventToEdit.StartDateTime = model.StartDateTime;
                eventToEdit.Duration = model.Duration;
                eventToEdit.Description = model.Description;
                eventToEdit.Address = model.Address;
                eventToEdit.City = model.City;
                eventToEdit.State = model.State;
                eventToEdit.Zip = zip > 9999 ? new Nullable<int>(zip) : null;
                eventToEdit.IsPublic = model.IsPublic;

                this.db.SaveChanges();
                this.AddNotification("Event edited.", NotificationType.INFO);
                return this.RedirectToAction("My");
            }

            return this.View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Delete(int id)
        {
            var eventToDelete = this.LoadEvent(id);
            if (eventToDelete == null)
            {
                this.AddNotification("Cannot delete event #" + id, NotificationType.ERROR);
                return this.RedirectToAction("My");
            }

            var model = EventInputModel.CreateFromEvent(eventToDelete);

            return this.View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, EventInputModel model)
        {
            var eventToDelete = this.LoadEvent(id);
            if (eventToDelete == null)
            {
                this.AddNotification("Cannot delete event #" + id, NotificationType.ERROR);
                return this.RedirectToAction("My");
            }

            this.eventsdb.Events.Remove(eventToDelete);
            this.eventsdb.SaveChanges();
            this.AddNotification("Event deleted.", NotificationType.INFO);
            return this.RedirectToAction("My");
        }

        [Authorize]
        public ActionResult MyUpcomingEvents()
        {
            var userId = this.User.Identity.GetUserId();

            var myEvents = eventsdb.EventInvites.Where(x => x.ToUser.Equals(userId, StringComparison.InvariantCultureIgnoreCase) && x.Event.StartDateTime >= DateTime.Today);
            var model = myEvents.OrderByDescending(x => x.Attending).ThenBy(x => x.Event.StartDateTime).ThenByDescending(x => x.Declined).Select(x => new MyUpcomingEventsViewModel()
            {
                InvitationId = x.EventInvitationId,
                Accepted = x.Attending,
                Declined = x.Declined,
                User = x.AspNetUser.FullName,
                Event = new EventViewModel() { 
                    Id = x.Event.Id,
                    Title = x.Event.Title,
                    StartDateTime = x.Event.StartDateTime,
                    Duration = x.Event.Duration,
                    Location = x.Event.Address + ", " + x.Event.City + ", " + x.Event.State + (x.Event.Zip > 9999 ? ", " + x.Event.Zip.ToString() : ""),
                    Author = x.Event.AspNetUser.FullName,
                }
            });
            
            return View("MyUpComingEventsView", model.ToList());
        }

        [Authorize]
        public ActionResult AcceptInvite(int id)
        {
            var evnt = eventsdb.EventInvites.Single(i => i.EventInvitationId == id);
            evnt.Attending = true;
            eventsdb.SaveChanges();
            
            return Redirect("~/Events/MyUpcomingEvents");
        }

        [Authorize]
        public ActionResult DeclineInvite(int id)
        {
            var evnt = eventsdb.EventInvites.Single(i => i.EventInvitationId == id);
            evnt.Declined = true;
            eventsdb.SaveChanges();

            return Redirect("~/Events/MyUpcomingEvents");
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

                return View("EventDetails", eventDetails);
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
                int zip;
                int.TryParse(eventResult.postal_code, out zip);

                result.Title = eventResult.title;
                result.Address = eventResult.address;
                result.City = eventResult.city;
                result.State = eventResult.region;
                result.Zip = zip;
                result.Latitude = eventResult.latitude;
                result.Longitude = eventResult.longitude;

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

                return View("EventDetails", result);
            }
        }

        private Event LoadEvent(int id)
        {
            var currentUserId = this.User.Identity.GetUserId();
            var isAdmin = this.IsAdmin();
            var eventToEdit = this.eventsdb.Events
                .Where(e => e.Id == id)
                .FirstOrDefault(e => e.AuthorId == currentUserId || isAdmin);
            return eventToEdit;
        }

        private void SaveImages(int eventId)
        {
            for (int i = 0; i < Request.Files.Count; i++)
            {
                if (Request.Files[i].ContentLength > 0 && Request.Files[i].IsImage())
                {
                    var path = Server.MapPath("~/EventImages/Event_" + eventId.ToString());

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    Request.Files[i].SaveAs(Path.Combine(path, Request.Files[i].FileName));

                    EventImage image = new EventImage();
                    image.EventId = eventId;
                    image.ImageName = Request.Files[i].FileName;
                    eventsdb.EventImages.Add(image);
                }
            }
            eventsdb.SaveChanges();
        }

        internal static EventViewModel[] AddImagesToOurEvents(IOrderedQueryable<Event> events)
        {
            var eventIds = events.Select(e => e.Id).ToArray();

            var eventViews = events.Select(EventViewModel.ViewModel).ToArray();

            //TODO: Change if implementing multiple image display
            var eventImages = (from e in events
                               select e.EventImages.FirstOrDefault()).ToArray();

            for (int i = 0; i < eventViews.Length; i++)
            {
                if (eventImages[i] != null)
                    eventViews[i].ImageURI = "http://localhost:9999/EventImages/Event_" + eventIds[i].ToString() + "/" + eventImages[i].ImageName;
                else
                    eventViews[i].ImageURI = "http://localhost:9999/EventImages/Default/img.jpg";
            }
            return eventViews;
        }

        private string[] resolveCoordinates(string address, string city, string state, string zip)
        {
            string result;

            using (var client = new HttpClient())
            {
                HttpResponseMessage response;

                try
                {
                    string apiKey = System.Configuration.ConfigurationManager.AppSettings["Google Maps API Key"];

                    client.BaseAddress = new Uri(@"https://maps.googleapis.com/maps/api/geocode/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    int i = 0;
                    if (int.TryParse(zip, out i) && i > 9999)
                    {
                        response = client.GetAsync(string.Format(@"json?address={0},+{1},+{2},+{3}&key={4}", address, city, state, zip, apiKey)).Result;
                    }
                    else
                    {
                        response = client.GetAsync(string.Format(@"json?address={0},+{1},+{2}&key={3}", address, city, state, apiKey)).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            if (result.Contains("\"location\"") && result.Contains("\"lng\"") && result.Contains("\"lat\""))
            {
                string[] arr = new string[2];
                int latStart = result.IndexOf("\"lat\" : ") + 8;
                int lonStart = result.IndexOf("\"lng\" : ") + 8;

                arr[0] = result.Substring(latStart, result.IndexOf(",", latStart) - latStart).Trim();
                arr[1] = result.Substring(lonStart, result.IndexOf("\n", lonStart) - lonStart).Trim();

                return arr;
            }
            else
                return null;
        }

        private IEnumerable<SelectListItem> GetStates()
        {
            List<SelectListItem> states = new List<SelectListItem>();

            states.Add(new SelectListItem { Text = "Alabama", Value = "AL" });
            states.Add(new SelectListItem { Text = "Alaska", Value = "AK" });
            states.Add(new SelectListItem { Text = "Arizona", Value = "AZ" });
            states.Add(new SelectListItem { Text = "Arkansas", Value = "AR" });
            states.Add(new SelectListItem { Text = "California", Value = "CA" });
            states.Add(new SelectListItem { Text = "Colorado", Value = "CO" });
            states.Add(new SelectListItem { Text = "Connecticut", Value = "CT" });
            states.Add(new SelectListItem { Text = "Delaware", Value = "DE" });
            states.Add(new SelectListItem { Text = "Florida", Value = "FL" });
            states.Add(new SelectListItem { Text = "Georgia", Value = "GA" });
            states.Add(new SelectListItem { Text = "Hawaii", Value = "HI" });
            states.Add(new SelectListItem { Text = "Idaho", Value = "ID" });
            states.Add(new SelectListItem { Text = "Illinois", Value = "IL" });
            states.Add(new SelectListItem { Text = "Indiana", Value = "IN" });
            states.Add(new SelectListItem { Text = "Iowa", Value = "IA" });
            states.Add(new SelectListItem { Text = "Kansas", Value = "KS" });
            states.Add(new SelectListItem { Text = "Kentucky", Value = "KY" });
            states.Add(new SelectListItem { Text = "Louisiana", Value = "LA" });
            states.Add(new SelectListItem { Text = "Maine", Value = "ME" });
            states.Add(new SelectListItem { Text = "Maryland", Value = "MD" });
            states.Add(new SelectListItem { Text = "Massachusetts", Value = "MA" });
            states.Add(new SelectListItem { Text = "Michigan", Value = "MI" });
            states.Add(new SelectListItem { Text = "Minnesota", Value = "MN" });
            states.Add(new SelectListItem { Text = "Mississippi", Value = "MS" });
            states.Add(new SelectListItem { Text = "Missouri", Value = "MO" });
            states.Add(new SelectListItem { Text = "Montana", Value = "MT" });
            states.Add(new SelectListItem { Text = "Nebraska", Value = "NE" });
            states.Add(new SelectListItem { Text = "Nevada", Value = "NV" });
            states.Add(new SelectListItem { Text = "New Hampshire", Value = "NH" });
            states.Add(new SelectListItem { Text = "New Jersey", Value = "NJ" });
            states.Add(new SelectListItem { Text = "New Mexico", Value = "NM" });
            states.Add(new SelectListItem { Text = "New York", Value = "NY" });
            states.Add(new SelectListItem { Text = "North Carolina", Value = "NC" });
            states.Add(new SelectListItem { Text = "North Dakota", Value = "ND" });
            states.Add(new SelectListItem { Text = "Ohio", Value = "OH" });
            states.Add(new SelectListItem { Text = "Oklahoma", Value = "OK" });
            states.Add(new SelectListItem { Text = "Oregon", Value = "OR" });
            states.Add(new SelectListItem { Text = "Pennsylvania", Value = "PA" });
            states.Add(new SelectListItem { Text = "Rhode Island", Value = "RI" });
            states.Add(new SelectListItem { Text = "South Carolina", Value = "SC" });
            states.Add(new SelectListItem { Text = "South Dakota", Value = "SD" });
            states.Add(new SelectListItem { Text = "Tennessee", Value = "TN" });
            states.Add(new SelectListItem { Text = "Texas", Value = "TX" });
            states.Add(new SelectListItem { Text = "Utah", Value = "UT" });
            states.Add(new SelectListItem { Text = "Vermont", Value = "VT" });
            states.Add(new SelectListItem { Text = "Virginia", Value = "VA" });
            states.Add(new SelectListItem { Text = "Washington", Value = "WA" });
            states.Add(new SelectListItem { Text = "West Virginia", Value = "WV" });
            states.Add(new SelectListItem { Text = "Wisconsin", Value = "WI" });
            states.Add(new SelectListItem { Text = "Wyoming", Value = "WY" });

            return states;
        }

        private IEnumerable<SelectListItem> GetFriends()
        {
            var userid = this.User.Identity.GetUserId();

            //Get Friendships
            var friendships = eventsdb.Friendships.Where(f => f.Friend1.Equals(userid, StringComparison.InvariantCultureIgnoreCase)
                                                           || f.Friend2.Equals(userid, StringComparison.InvariantCultureIgnoreCase)).ToList();

            //Get Friends
            List<Events.Data.AspNetUser> friends = friendships.Where(f => f.Friend1 != userid).Select(f => f.AspNetUser).ToList();
            friends.AddRange(friendships.Where(f => f.Friend2 != userid).Select(f => f.AspNetUser1).ToList());


            IEnumerable<SelectListItem> result = friends.Select(f => new SelectListItem()
            {
                Text = f.FullName,
                Value = f.Id
            });

            return result;
        }
    }
}