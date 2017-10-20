using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using WebHook.Net.Models;

namespace WebHook.Net.Controllers
{
    public class BlogEventsController : Controller
    {
        private readonly SshConfig _sshConfig;

        public BlogEventsController(IOptions<SshConfig> sshConfig)
        {
            _sshConfig = sshConfig.Value;
        }

        [HttpGet]
        [HttpPost]
        public IActionResult Index([FromBody]JObject data)
        {
            if (data == null)
                return BadRequest("Data payload cannot be empty");

            var converter = new ExpandoObjectConverter();
            var json = data.ToString();
            dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(json);


            using (var client = new SshClient(_sshConfig.Host, _sshConfig.Username, _sshConfig.Password))
            {
                string repoName = obj.repository.name;
                string repoUrl = obj.repository.clone_url;

                string result = "";
                client.Connect();
                result += client.RunCommand("rm -rf /tmp/" + repoName).Execute();
                result += client.RunCommand("git clone " + repoUrl + " /tmp/" + repoName).Execute();
                result += client.RunCommand("cd /tmp/" + repoName + "/ && npm install").Execute();
                result += client.RunCommand("cd /tmp/" + repoName + "/ && dotnet publish -o /tmp/" + repoName + "_publish/").Execute();
                Debug.Print(result);
            }
            return Ok(new { Ok = true });
        }

    }
}