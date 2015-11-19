using Events.Data;
using Events.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace Events.Web.Controllers
{
    public class CommentController : BaseController
    {
        // GET: Comment
        public ActionResult Index()
        {
            return View();
        }

        // GET: Comment/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Comment/Create
        public ActionResult Create(int id, string eventfulId, bool isEventfulEvent)
        {
            ViewBag.id = id;
            ViewBag.eventfulId = eventfulId;
            ViewBag.isEventfulEvent = isEventfulEvent;

            return View("AddEventfulComment");
        }

         //POST: Comment/Create
        [HttpPost]
        public ActionResult Create(int id, string eventfulId, bool isEventfulEvent, string Text)//FormCollection collection)
        {                
            try
            {
                if (isEventfulEvent)
                {
                    string guid = User.Identity.GetUserId();
                    //Get the user
                    Events.External.AspNetUser user = this.eventfulDb.AspNetUsers.Single(u => u.Id.Equals(guid, StringComparison.InvariantCultureIgnoreCase));

                    //Create the comment
                    EventfulComment comment = new EventfulComment();
                    comment.AuthorId = user.Id;
                    comment.Date = DateTime.Now;
                    comment.EventfulId = eventfulId;
                    comment.Text = Text;

                    //Save
                    user.EventfulComments.Add(comment);
                    this.eventfulDb.SaveChanges();
                }
                else
                {
                    //Create the comment
                    Comment comment = new Comment();
                    comment.Event = this.eventsdb.Events.Single(e => e.Id == id);
                    comment.AuthorId = User.Identity.GetUserId();
                    comment.Date = DateTime.Now;
                    comment.EventId = id;
                    comment.Text = Text;

                    //Save
                    this.eventsdb.Comments.Add(comment);
                    this.eventsdb.SaveChanges();
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                return View();
            }
        }

        // GET: Comment/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Comment/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Comment/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Comment/Delete/5
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
