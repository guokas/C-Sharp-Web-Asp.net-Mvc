using ClientCheckWeb.Controllers;
using Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcApplicationSimpleExample.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.firstname = GetPara("firstname");
            ViewBag.lastname = GetPara("lastname");
            return View();
        }
        public ActionResult say()
        {
            return Content("hello world","text/plain");
        }
    }
}
