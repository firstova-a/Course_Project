using JwtTest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JwtTest.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> logger;

        public HomeController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            ClaimsIdentity cookieClaims = User.Identities.FirstOrDefault(cc => cc.AuthenticationType == "ApplicationCookie");
            bool authenticated = cookieClaims != null && cookieClaims.IsAuthenticated;
            if (authenticated)
            {
                return Redirect("/Account/AllListBlog");
            }
            else
            {
                return Redirect("/Account/AllListBlog");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
