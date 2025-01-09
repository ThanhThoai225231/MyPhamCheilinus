using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using MyPhamCheilinus.Models;
using System.Diagnostics;
using System.Web;
using X.PagedList;
using static MyPhamCheilinus.Controllers.HomeController;
using Microsoft.AspNetCore.Authorization;


namespace MyPhamCheilinus.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ChatController : Controller
    {


        private readonly _2023MyPhamContext _context;
        

        public ChatController(_2023MyPhamContext context)
        {
            _context = context;
            
        }

        public IActionResult Index()
        {         
            return View();
        }
    }
}
