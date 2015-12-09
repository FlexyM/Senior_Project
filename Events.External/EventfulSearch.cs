using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Serialization;
using System.IO;

using System.Web.Helpers;

namespace Events.External
{
    public class EventfulSearch
    {
        private readonly string apiKey;
        public EventfulSearch()
        {
            apiKey = System.Configuration.ConfigurationManager.AppSettings["Eventful API Key"];
        }

        public string Id { get; set; }

        public string Keyword { get; set; }

        public string Location { get; set; }

        public string Date { get; set; }

        public string SortOrder { get; set; }

        //With XML
        public List<EventfulEvent> Search()
        {
            search searchResult = null;

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(@"http://api.eventful.com/rest/events/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    HttpResponseMessage response = client.GetAsync(string.Format(@"search?app_key={0}&location={1}&q={2}&date={3}&page_size=30", apiKey, Location, Keyword, Date)).Result;

                    XmlSerializer serializer = new XmlSerializer(typeof(search));
                    using (Stream stream = response.Content.ReadAsStreamAsync().Result)
                    {
                        searchResult = (search)serializer.Deserialize(stream);
                    }

                    return searchResult.events.ToList();

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        //JSON
        public dynamic GetEventfulDetails()
        {
            
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(@"http://api.eventful.com/json/events/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = client.GetAsync(string.Format(@"get?app_key={0}&id={1}", apiKey, Id)).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string stream = response.Content.ReadAsStringAsync().Result;
                        return Json.Decode(stream);
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}