namespace Events.Web.Models
{
    using System;
    using System.Linq.Expressions;

    using Events.Data;

    public class EventViewModel
    {
        private bool isEventfulEvent = false;

        public int Id { get; set; }

        public String EventfulId { get; set; }

        public string Title { get; set; }

        public DateTime StartDateTime { get; set; }

        public TimeSpan? Duration { get; set; }

        public string Author { get; set; }

        public string Location { get; set; }

        public bool IsEventfultEvent { get { return isEventfulEvent; } set { isEventfulEvent = value; } }

        public string ImageURI { get; set; }

        public static Expression<Func<Event, EventViewModel>> ViewModel
        {
            get
            {
                return e => new EventViewModel()
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartDateTime = e.StartDateTime,
                    Duration = e.Duration,
                    Location = e.Address + ", " + e.City + ", " + e.State + (e.Zip > 9999 ? ", " + e.Zip.ToString() : ""),
                    Author = e.AspNetUser.FullName,
                };
            }
        }
    }
}