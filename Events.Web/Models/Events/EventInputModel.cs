namespace Events.Web.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    using Events.Data;
    using System.Collections.Generic;
    using System.Web;
    using System.IO;
    using System.Text.RegularExpressions;

    public class EventInputModel
    {
        [Required(ErrorMessage = "Event title is required.")]
        [StringLength(200, ErrorMessage = "The {0} must be between {2} and {1} characters long.", 
            MinimumLength = 1)]
        [Display(Name = "Title *")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Date and Time is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date and Time *")]
        public DateTime StartDateTime { get; set; }

        public TimeSpan? Duration { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(250)]
        [Display(Name = "Address *")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [MaxLength(150)]
        [Display(Name = "City *")]
        public string City { get; set; }

        [Required(ErrorMessage = "State is required.")]
        [MaxLength(2)]
        [Display(Name = "State")]
        public string State { get; set; }

        [RegularExpression(@"^\d{5}$", ErrorMessage= "Please enter a 5 digit zip code.")]
        [Display(Name = "Zip Code")]
        [MaxLength(5)]
        public string Zip { get; set; }

        [Display(Name = "Is Public?")]
        public bool IsPublic { get; set; }

        public static EventInputModel CreateFromEvent(Event e)
        {
            return new EventInputModel()
            {
                Title = e.Title,
                StartDateTime = e.StartDateTime,
                Duration = e.Duration,
                Address = e.Address,
                City = e.City,
                State = e.State,
                Zip = e.Zip.GetValueOrDefault() > 9999 ? e.Zip.GetValueOrDefault().ToString() : string.Empty,
                Description = e.Description,
                IsPublic = e.IsPublic
            };
        }
    }

    public static class HttpPostedFileBaseExtensions
    {
        public const int ImageMinimumBytes = 512;

        public static bool IsImage(this HttpPostedFileBase postedFile)
        {
            //-------------------------------------------
            //  Check the image mime types
            //-------------------------------------------
            if (postedFile.ContentType.ToLower() != "image/jpg" &&
                        postedFile.ContentType.ToLower() != "image/jpeg" &&
                        postedFile.ContentType.ToLower() != "image/pjpeg" &&
                        postedFile.ContentType.ToLower() != "image/gif" &&
                        postedFile.ContentType.ToLower() != "image/x-png" &&
                        postedFile.ContentType.ToLower() != "image/png")
            {
                return false;
            }

            //-------------------------------------------
            //  Check the image extension
            //-------------------------------------------
            if (Path.GetExtension(postedFile.FileName).ToLower() != ".jpg"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".png"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".gif"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".jpeg")
            {
                return false;
            }

            //-------------------------------------------
            //  Attempt to read the file and check the first bytes
            //-------------------------------------------
            try
            {
                if (!postedFile.InputStream.CanRead)
                {
                    return false;
                }

                if (postedFile.ContentLength < ImageMinimumBytes)
                {
                    return false;
                }

                byte[] buffer = new byte[512];
                postedFile.InputStream.Read(buffer, 0, 512);
                string content = System.Text.Encoding.UTF8.GetString(buffer);
                if (Regex.IsMatch(content, @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            //-------------------------------------------
            //  Try to instantiate new Bitmap, if .NET will throw exception
            //  we can assume that it's not a valid image
            //-------------------------------------------

            try
            {
                using (var bitmap = new System.Drawing.Bitmap(postedFile.InputStream))
                {
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}