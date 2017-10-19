using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using WebHook.Net.Models;

namespace WebHook.Net.Controllers
{
    public class BlogEventsController : Controller
    {
        [HttpGet]
        [HttpPost]
        public IActionResult Index([FromBody]JObject data)
        {
            // if (string.IsNullOrWhiteSpace(data))
            //     return BadRequest("Data payload cannot be empty");

            // var converter = new ExpandoObjectConverter();    
            dynamic obj = data.ToObject<ExpandoObject>();

            using (var client = new SshClient("gollum", "root", "%Skji784"))
            {
                client.Connect();
                client.RunCommand("ls -hal").Execute();
                client.RunCommand("rm -rf /tmp/" + obj.payload.repository.name).Execute();
                client.RunCommand("git clone " + obj.payload.repository.clone_url + " /tmp/" + obj.payload.repository.name).Execute();
                client.RunCommand(" && cd /tmp/" + obj.payload.repository.name + "/").Execute();
                client.RunCommand("ls -hal").Execute();
                client.RunCommand("npm install").Execute();
                client.RunCommand("dotnet publish -o ./publish/").Execute();
            }
            return Ok(new { Ok = true });
        }

    }
}