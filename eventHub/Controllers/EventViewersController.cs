using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eventHub.Data;
using eventHub.Models;

namespace eventHub.Controllers
{
    public class EventViewersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public EventViewersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

public async Task<IActionResult> Index(string sortOrder = null)
{
    var user = await _userManager.GetUserAsync(User);
    var eventsQuery = _context.Event.Include(e => e.User).AsQueryable();

    if (user != null)
    {
        var userInterests = await _context.PersonalEvent
            .Where(pe => pe.UserId == user.Id)
            .Select(pe => pe.EventId)
            .ToListAsync();

        eventsQuery = eventsQuery.Where(e => !userInterests.Contains(e.Id));
    }

    eventsQuery = ApplySorting(eventsQuery, sortOrder);

    var events = await eventsQuery.ToListAsync();

    var eventViewers = events.Select(e => new EventViewer
    {
        Event = e,
        User = e.User
    }).ToList();

    ViewBag.SortOrder = sortOrder;

    return View(eventViewers);
}

public async Task<IActionResult> Search(string searchString, string sortOrder = null)
 {
     var user = await _userManager.GetUserAsync(User);
     var eventsQuery = _context.Event.Include(e => e.User).AsQueryable();
     if (user != null)
     {
         var userInterests = await _context.PersonalEvent
             .Where(pe => pe.UserId == user.Id)
             .Select(pe => pe.EventId)
             .ToListAsync();

         eventsQuery = eventsQuery.Where(e => !userInterests.Contains(e.Id));
     }


     if (!string.IsNullOrEmpty(searchString))
     {
         eventsQuery = eventsQuery.Where(e =>
             e.Title.Contains(searchString) ||
             e.Description.Contains(searchString) ||
             e.Location.Contains(searchString) ||
             e.User.UserName.Contains(searchString));
     }

     eventsQuery = ApplySorting(eventsQuery, sortOrder);

     var searchedEvents = await eventsQuery.ToListAsync();

     var eventViewers = searchedEvents.Select(e => new EventViewer
     {
         Event = e
     }).ToList();

     ViewBag.SearchString = searchString;
     ViewBag.SortOrder = sortOrder;

     return View("Index", eventViewers);
 }

private IQueryable<Event> ApplySorting(IQueryable<Event> eventsQuery, string sortOrder)
{
    return sortOrder switch
    {
        "asc" => eventsQuery.OrderBy(e => e.Start),
        "desc" => eventsQuery.OrderByDescending(e => e.Start),
        _ => eventsQuery
    };
}


        public async Task<IActionResult> Interested(int eventId)
        {
            IdentityUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            var existingInterest = await _context.PersonalEvent
                .FirstOrDefaultAsync(ui => ui.UserId == user.Id && ui.EventId == eventId);

            if (existingInterest == null)
            {
                var userInterest = new PersonalEvent
                {
                    UserId = user.Id,
                    EventId = eventId
                };

                _context.PersonalEvent.Add(userInterest);
                await _context.SaveChangesAsync();
            }

            //return RedirectToAction("Confirmation");
            return View("Confirmation");
        }

        // Akcja do usuwania zainteresowania użytkownika danym wydarzeniem
        //public async Task<IActionResult> NotInterested(int eventId)
        //{
        //    IdentityUser user = await _userManager.FindByNameAsync(User.Identity.Name);

        //    var existingInterest = await _context.PersonalEvent
        //        .FirstOrDefaultAsync(ui => ui.UserId == user.Id && ui.EventId == eventId);

        //    if (existingInterest != null)
        //    {
        //        _context.PersonalEvent.Remove(existingInterest);
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction("Index");
        //}
    }
}
