﻿using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MvvmTools.Shared.Models;
using MvvmTools.Web.Models;

namespace MvvmTools.Web.Controllers.Api
{
    [RequireHttps]
    [System.Web.Mvc.Authorize]
    public class TemplatesController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: api/Templates
        public IQueryable<Template> GetTemplates()
        {
            var currentUserId = User.Identity.GetUserId();
            return from t in db.MvvmTemplates
                where currentUserId == t.ApplicationUserId
                select new Template
                {
                    Id = t.Id,
                    Enabled = t.Enabled,
                    Author = t.ApplicationUser.Author,
                    Name = t.Name,
                    Language = t.Language,
                    Category = t.MvvmTemplateCategory.Name,
                    Tags = t.Tags,
                    ViewModel = t.ViewModel,
                    View = t.View
                };
        }

        private Template CreateTemplateFromMvvmTemplate(MvvmTemplate mt)
        {
            return new Template(mt.Id, mt.Enabled, mt.ApplicationUser.Author, mt.Name, mt.Language, mt.MvvmTemplateCategory.Name, mt.Tags, mt.ViewModel, mt.View);
        }

        // GET: api/Templates/5
        [ResponseType(typeof(Template))]
        public async Task<IHttpActionResult> GetTemplate(int id)
        {
            var mvvmTemplate = await db.MvvmTemplates.FindAsync(id);
            if (mvvmTemplate == null)
                return NotFound();

            return Ok(CreateTemplateFromMvvmTemplate(mvvmTemplate));
        }

        // PUT: api/Templates/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutTemplate(int id, Template template)
        {
            // There are no attributes on the DTO so this statement does nothing.
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            if (id != template.Id ||
                string.IsNullOrWhiteSpace(template.Name) ||
                string.IsNullOrWhiteSpace(template.Author) ||
                string.IsNullOrWhiteSpace(template.Category) ||
                template.Tags == null ||
                template.View == null ||
                template.ViewModel == null)
                return BadRequest();

            // Locate record.
            var mvvmTemplate = await db.MvvmTemplates.FindAsync(id);
            if (mvvmTemplate == null)
                return NotFound();

            mvvmTemplate.Id = template.Id;
            mvvmTemplate.Enabled = template.Enabled;
            var author = await db.Users.FirstOrDefaultAsync(u => u.Author.ToLower() == template.Author.ToLower());
            if (author == null)
                return BadRequest();
            mvvmTemplate.ApplicationUserId = author.Id;
            mvvmTemplate.Name = template.Name;
            mvvmTemplate.Language = template.Language;
            var category = await db.MvvmTemplateCategories.FirstOrDefaultAsync(c => c.Name.ToLower() == template.Category.ToLower());
            if (category == null)
                return BadRequest();
            mvvmTemplate.MvvmTemplateCategoryId = category.Id;
            mvvmTemplate.Tags = template.Tags;
            mvvmTemplate.ViewModel = template.ViewModel;
            mvvmTemplate.View = template.View;

            await db.SaveChangesAsync();
            
            return StatusCode(HttpStatusCode.NoContent);
        }
        
        // POST: api/Templates
        [ResponseType(typeof(MvvmTemplate))]
        public async Task<IHttpActionResult> PostTemplate(MvvmTemplate mvvmTemplate)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.MvvmTemplates.Add(mvvmTemplate);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = mvvmTemplate.Id }, mvvmTemplate);
        }

        // DELETE: api/Templates/5
        [ResponseType(typeof(MvvmTemplate))]
        public async Task<IHttpActionResult> DeleteTemplate(int id)
        {
            var mvvmTemplate = await db.MvvmTemplates.FindAsync(id);
            if (mvvmTemplate == null)
                return NotFound();

            db.MvvmTemplates.Remove(mvvmTemplate);
            await db.SaveChangesAsync();

            return Ok(mvvmTemplate);
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