using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using WebHook.Net.Models;

namespace WebHook.Net.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [HttpPost]
        public IActionResult Index(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return BadRequest("Data payload cannot be empty");
            using (var client = new SshClient("gollum", "root", "%Skji784"))
            {
                client.Connect();
                client.RunCommand("ls -hal").Execute();
            }
            return Ok(new { Ok = true });
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
