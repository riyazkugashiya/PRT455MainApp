using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using WebGrease.Css.Ast.Selectors;

namespace EventManagementSystem
{
    public class tblEventDetailsController : Controller
    {
        private dbEventManagementSystemEntities1 db = new dbEventManagementSystemEntities1();

        // GET: tblEventDetails
        [Authorize]
        public ActionResult Index()
        {
            var strUserId = User.Identity.GetUserId();
            var tblEventDetail = db.tblEventDetails.Where(t => t.UserID == strUserId);
            return View(tblEventDetail.ToList());

        }

        [Authorize]
        public ActionResult InvitePeople(string strUserSearch, int? id)
        {
            Session["EventID"] = id;
            TempData["EventID"] = id;
            var strUserId = User.Identity.GetUserId();

            if (strUserSearch != "" && strUserSearch != null)
            {
                Session["SearchString"] = strUserSearch;
                var objtblUserDetail = db.AspNetUsers.Where(t => (t.Id != strUserId) && (t.FirstName.Contains(strUserSearch) || t.MiddleName.Contains(strUserSearch) || t.LastName.Contains(strUserSearch))).ToList();
                var eventinfo = db.tblEventInvitationInfoes.Where(t => t.EventID == id).ToList();

                if (objtblUserDetail.Count != 0)
                {
                    if (eventinfo.Count != 0)
                    {
                        for (int i = 0; i < objtblUserDetail.Count; i++)
                        {
                            for (int j = 0; j < eventinfo.Count; j++)
                            {
                                if (eventinfo[j].InvitedID == objtblUserDetail[i].Id)
                                {
                                    objtblUserDetail.Remove(objtblUserDetail[i]);
                                }

                            }
                        }
                    }

                }
                return View(objtblUserDetail.ToList());
            }
            else
            {
                var all = db.AspNetUsers.Where(t => t.Id != strUserId).ToList(); 
                var eventinfo = db.tblEventInvitationInfoes.Where(t=> t.EventID == id).ToList();
                if (eventinfo.Count != 0)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        for (int j = 0; j < eventinfo.Count; j++)
                        {
                            if (eventinfo[j].InvitedID == all[i].Id)
                            {
                                all.Remove(all[i]);
                                eventinfo.Remove(eventinfo[j]);
                                i--;
                            }

                        }
                    }
                 }
                

                return View(all);
                
            }
        }
        
        [Authorize]
        public ActionResult SendInvitation(string id)
        {
            var strUserId = User.Identity.GetUserId();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                tblEventInvitationInfo objTblEventInvitationInfo = new tblEventInvitationInfo();

               // int EventId = Convert.ToInt32(TempData["EventID"]);
                int EventId = Convert.ToInt32(Session["EventID"]);
                objTblEventInvitationInfo.EventID = EventId;
                objTblEventInvitationInfo.SenderID = strUserId;
                objTblEventInvitationInfo.InvitedID = Convert.ToString(id);
                db.tblEventInvitationInfoes.Add(objTblEventInvitationInfo);
                db.SaveChanges();

                // For sending mail of invitataion to invite person 
                // Getting some information

                var inviteUserId = db.AspNetUsers.Where(t => t.Id == id).FirstOrDefault();
                var senderName = db.AspNetUsers.Where(q => q.Id == strUserId).FirstOrDefault();
                var eventInfo = db.tblEventDetails.Where(e => e.ID == EventId).FirstOrDefault();

                MailMessage mail = new MailMessage();
                mail.To.Add(inviteUserId.Email);
                mail.From = new MailAddress(senderName.Email);
                mail.Subject = " Invitation from " + senderName.FirstName + " " + senderName.MiddleName + " " + senderName.LastName +" for " + eventInfo.EventTitle;
                mail.Body = "Hi <b>"+ inviteUserId.FirstName + "</b>," + "<br/><br/><b>" + senderName.FirstName +"</b> invite you for " + eventInfo.EventTitle 
                             + "<br/><br/> Please see below for all event related information. <br/><br/> <b>Event:</b> " + eventInfo.EventTitle +
                             "<br/><br/> <b>Venue:</b> " + eventInfo.Venue + "<br/><br/> <b>Date and Time:</b> " + eventInfo.DateTime
                             + "<br/><br/> <b>Descritption:</b> " + eventInfo.Description + "<br/><br/> To give response to this event please go to your account on www.eventmanagementsystem.com"
                             + "<br/><br/><br/> By Event Management Team";

                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential("riyazkugashiya@gmail.com", "obtflqemvqjdtkcl");
                smtp.EnableSsl = true;
                smtp.Send(mail);

                return RedirectToAction("InvitePeople/"+ EventId);
            }
        }

        [Authorize]
        public ActionResult InvitedResponse()
        {
            int id = (int)TempData["EventID"];

            var tblInvitedPeople = db.tblEventInvitationInfoes.Where(t => t.EventID == id);
            return PartialView(tblInvitedPeople.ToList());

        }

        public ActionResult EventInvitation()
        {
            var strUserID = User.Identity.GetUserId();
            
            var query = (from invite in db.tblEventInvitationInfoes.Where(t => t.InvitedID == strUserID)
                join allevent in db.tblEventDetails on invite.EventID equals allevent.ID
                select allevent).ToList();

            return View(query);
        }
        
        // GET: tblEventDetails/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblEventDetail tblEventDetail = db.tblEventDetails.Find(id);
            if (tblEventDetail == null)
            {
                return HttpNotFound();
            }
            return View(tblEventDetail);
        }

        
        // GET: tblEventDetails/Create
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.UserID = new SelectList(db.AspNetUsers, "Id", "FirstName");
            return View();
        }

        // POST: tblEventDetails/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Create([Bind(Include = "ID,UserID,EventTitle,Venue,DateTime,Description,Picture")] tblEventDetail tblEventDetail, HttpPostedFileBase file)
        {
            tblEventDetail.UserID = User.Identity.GetUserId();
            if(file != null)
            { 
                if(file.ContentLength >= 0)
                {
                    var filename = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/EventUploadFiles/"), filename);
                    file.SaveAs(path);
                    tblEventDetail.Picture = filename;
                }
            }
            if (ModelState.IsValid)
                {
                    db.tblEventDetails.Add(tblEventDetail);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
           
            ViewBag.UserID = new SelectList(db.AspNetUsers, "Id", "FirstName", tblEventDetail.UserID);
            return View(tblEventDetail);
        }


        public ActionResult _ViewEventResponsePartial(int? id)
        {
            var strUserID = User.Identity.GetUserId();

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //tblEventDetail tblEventDetail = db.tblEventDetails.Find(id);
            var tblobjEvent = db.tblEventInvitationInfoes.Where(t=> t.EventID == id && t.InvitedID == strUserID).ToList();
            tblEventInvitationInfo tblobj = db.tblEventInvitationInfoes.Find(tblobjEvent[0].ID);
            if (tblobj == null)
            {
                return HttpNotFound();
            }
            TempData["EventInviInfoID"] = tblobj.ID;
            tblobj.Response = true;
           // ViewBag.UserID = new SelectList(db.AspNetUsers, "Id", "FirstName", tblobj.UserID);
            return PartialView("_ViewEventResponsePartial", tblobj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult _ViewEventResponsePartial([Bind(Include = "ID,EventID,SenderID,InvitedID,Response,Feedback")] tblEventInvitationInfo tblobjEventinfo)
        {
             
            if (ModelState.IsValid)
            {
                tblobjEventinfo.ID = (int)TempData["EventInviInfoID"];
                db.Entry(tblobjEventinfo).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("EventInvitation");
            }
            //ViewBag.UserID = new SelectList(db.AspNetUsers, "Id", "FirstName", tblEventDetail.UserID);
            return View(tblobjEventinfo);
        }

        // GET: tblEventDetails/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblEventDetail tblEventDetail = db.tblEventDetails.Find(id);
            if (tblEventDetail == null)
            {
                return HttpNotFound();
            }
           // tblEventDetail.DateTime = DateTime.Parse(tblEventDetail.DateTime.ToString("g"));
            ViewBag.UserID = new SelectList(db.AspNetUsers, "Id", "FirstName", tblEventDetail.UserID);
            return View(tblEventDetail);
        }

        // POST: tblEventDetails/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "ID,UserID,EventTitle,Venue,DateTime,Description,Picture")] tblEventDetail tblEventDetail, HttpPostedFileBase file)
        {
            var strUserId = User.Identity.GetUserId();
            tblEventDetail.UserID = strUserId;
            if (file != null)
            {
                if (file.ContentLength >= 0)
                {
                    var filename = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/EventUploadFiles/"), filename);
                    file.SaveAs(path);
                    tblEventDetail.Picture = filename;
                }
            }
            else
            {
                //tblEventDetail.Picture =
            }
            if (ModelState.IsValid)
            {
                db.Entry(tblEventDetail).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserID = new SelectList(db.AspNetUsers, "Id", "FirstName", tblEventDetail.UserID);
            return View(tblEventDetail);
        }

        // GET: tblEventDetails/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblEventDetail tblEventDetail = db.tblEventDetails.Find(id);
            if (tblEventDetail == null)
            {
                return HttpNotFound();
            }
            return View(tblEventDetail);
        }

        // POST: tblEventDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteConfirmed(int id)
        {

            //var tblEventInfoVar = db.tblEventInvitationInfoes.Where(e => e.EventID == id).ToList();
            //tblEventInvitationInfo tblEventInfo = tblEventInfoVar.ToList(); 
            //db.tblEventInvitationInfoes.Remove(tblEventInfo);
            db.tblEventInvitationInfoes.RemoveRange(db.tblEventInvitationInfoes.Where(c => c.EventID == id));
            db.SaveChanges();

            tblEventDetail tblEventDetail = db.tblEventDetails.Find(id);
            db.tblEventDetails.Remove(tblEventDetail);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
