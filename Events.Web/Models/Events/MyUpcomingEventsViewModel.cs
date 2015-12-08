using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Events.Web.Models
{
    public class MyUpcomingEventsViewModel
    {
        public int InvitationId { get; set; }
        public EventViewModel Event { get; set; }
        public string User { get; set; }
        public bool Accepted { get; set; }
        public bool Declined { get; set; }
    }
}