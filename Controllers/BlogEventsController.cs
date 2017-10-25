using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private static SshConfig _sshConfig;
        private readonly ILogger<BlogEventsController> _logger;

        public BlogEventsController(ILogger<BlogEventsController> logger, IOptions<SshConfig> sshConfig)
        {
            _sshConfig = sshConfig.Value;
            _logger = logger;
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
            string repoName = obj.repository.name;
            string repoUrl = obj.repository.clone_url;

            ThreadPool.QueueUserWorkItem(delegate {
                BuildApplication(repoName, repoUrl);
                DeployApplication(repoName, repoUrl);
            });

            return Ok(new { Ok = true });
        }
        
        private static void BuildApplication(string repoName, string repoUrl)
        {
            ("rm -rf /tmp/" + repoName).Bash();
            ("git clone " + repoUrl + " /tmp/" + repoName).Bash();
            ("cd /tmp/" + repoName + "/ && npm install").Bash();
            ("cd /tmp/" + repoName + "/ && dotnet publish -o /tmp/" + repoName + "_publish/").Bash();
        }

        private static void DeployApplication(string repoName, string repoUrl)
        {
            if(!System.IO.Directory.Exists("./sshkeys"))
                ("mkdir ./sshkeys/").Bash();
            
            if(!System.IO.File.Exists("./sshkeys/id_rsa"))
                ("cp ~/.ssh/id_rsa* ./sshkeys/").Bash();

            using (var sshClient = new SshClient(_sshConfig.Host, _sshConfig.Username, new []{new PrivateKeyFile(_sshConfig.KeyFile)}))
            {
                sshClient.Connect();
                using (var scpClient = new ScpClient(_sshConfig.Host, _sshConfig.Username, new []{new PrivateKeyFile(_sshConfig.KeyFile)}))
                {
                    scpClient.Connect();
                    Debug.Print(sshClient.RunCommand("ls -hal /tmp/").Execute());
                    sshClient.RunCommand("rm -rf /tmp/" + repoName).Execute();
                    scpClient.Upload(new DirectoryInfo("/tmp/" + repoName), "/tmp/" + repoName);
                    scpClient.Disconnect();
                }                
                sshClient.Disconnect();
            }
        }
    }
}