using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Events.Web.Models;

namespace Events.Web.Extensions
{
    public static class EventfulExtentions
    {
        public static IEnumerable<EventViewModel> ConvertToEventViewModel(this List<EventfulEvent> EventfulEvents)
        {
            List<EventViewModel> model = new List<EventViewModel>();

            foreach (EventfulEvent events in EventfulEvents)
            {
                string url = null;

                var images = events.image as System.Xml.XmlNode[];

                if (images != null)
                {
                    url = images.Any(e => string.Equals(e.Name, "medium", StringComparison.InvariantCultureIgnoreCase)) ?
                        images.Single(e => string.Equals(e.Name, "medium", StringComparison.InvariantCultureIgnoreCase)).InnerText : null;
                }

                model.Add(new EventViewModel
                        {
                            EventfulId = events.id,
                            Author = events.owner,
                            Location = events.venue_address,
                            StartDateTime = DateTime.Parse(events.start_time),
                            Title = events.title,
                            IsEventfultEvent = true,
                            ImageURI = url
                        });
            }

            return model.AsEnumerable();
        }

        public static IEnumerable<EventViewModel> AddMissingImagesToTheirEvents(this IOrderedEnumerable<EventViewModel> EventfulEvents)
        {
            foreach(var evnt in EventfulEvents)
            {
                if(string.IsNullOrEmpty(evnt.ImageURI))
                    evnt.ImageURI = "http://localhost:9999/EventImages/Default/img.jpg";
            }
            return EventfulEvents;
        }
    }
}