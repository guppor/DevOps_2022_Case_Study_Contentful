﻿using Contentful.Core;
using Contentful.Core.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reload.Data;
using Reload.Models.Contentful;
using Reload.Models.DataTables;
using X.PagedList;

namespace Reload.Controllers
{
    public class CourseController : Controller
    {
        private readonly IContentfulClient _client;

        private readonly ApplicationDbContext _db;

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;


        public CourseController(IContentfulClient client, ApplicationDbContext db, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _client = client;
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> DynamicCourse(string courseName, string chapterIDs, int? page)
        {
            ViewData["courseName"] = courseName;
            ViewData["chapterIDs"] = chapterIDs;

            string[] seperatedchapterId = chapterIDs.Split(",");

            var builder = QueryBuilder<Chapter>.New.ContentTypeIs("chapter").OrderBy("sys.createdAt");
            var chapters = await _client.GetEntries(builder);

            List<Chapter> courseChapters = new List<Chapter>();
            foreach (var chapter in chapters)
            {
                string? id = chapter.Sys?["id"];
                if (seperatedchapterId.Any(id.Contains))
                {
                    courseChapters.Add(chapter);
                }
            }

            int pageSize = 1;
            int pageNumber = (page ?? 1);

            if (_signInManager.IsSignedIn(User))
            {
                IdentityUser user = await GetCurrentUserAsync();
                var courses = _db.CourseProgress.Where(c => c.UserId.Equals(user.Id) && c.CourseName.Equals(courseName)).ToList();

                if (courses.Count() == 0)
                {
                    CourseProgress courseProgress = new CourseProgress(courseName, pageNumber, user.Id);
                    _db.CourseProgress.Add(courseProgress);
                }
                else
                {
                    courses[0].ChapterIdx = pageNumber - 1;
                }
                _db.SaveChanges();
            }

            return View(courseChapters.ToArray().ToPagedList(pageNumber, pageSize));
        }

        private Task<IdentityUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

    }
}
