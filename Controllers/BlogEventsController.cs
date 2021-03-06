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
            string tempDirName = Guid.NewGuid().ToString();

            ThreadPool.QueueUserWorkItem(delegate {
                BuildApplication(tempDirName, repoName, repoUrl);
                DeployApplication(tempDirName, repoName, repoUrl);
            });

            return Ok(new { Ok = true });
        }
        
        private static void BuildApplication(string tempDirName, string repoName, string repoUrl)
        {
            ($"rm -rf /tmp/{tempDirName}/{repoName}").Bash();
            ($"git clone {repoUrl}/tmp/{tempDirName}/{repoName}").Bash();
            ($"cd /tmp/{tempDirName}/{repoName}/ && npm install").Bash();
            ($"cd /tmp/{tempDirName}/{repoName}/ && dotnet publish -o /tmp/{repoName}_publish/").Bash();
            ($"cd /tmp/{tempDirName}/{repoName}/ && tar -zcvf /tmp/{tempDirName}/{repoName}_publish.tar.gz /tmp/{tempDirName}/{repoName}_publish").Bash();
        }

        private static void DeployApplication(string tempDirName, string repoName, string repoUrl)
        {
            if(!Directory.Exists("./sshkeys"))
                ($"mkdir ./sshkeys/").Bash();
            
            if(!System.IO.File.Exists("./sshkeys/id_rsa"))
                ($"cp ~/.ssh/id_rsa* ./sshkeys/").Bash();

            using (var sshClient = new SshClient(_sshConfig.Host, _sshConfig.Username, new []{new PrivateKeyFile(_sshConfig.KeyFile)}))
            {
                sshClient.Connect();
                using (var scpClient = new ScpClient(_sshConfig.Host, _sshConfig.Username, new []{new PrivateKeyFile(_sshConfig.KeyFile)}))
                {
                    scpClient.Connect();
                    Debug.Print(sshClient.RunCommand("ls -hal /tmp/").Execute());
                    sshClient.RunCommand($"rm -rf /tmp/{repoName}").Execute();
                    scpClient.Upload(new FileInfo($"/tmp/{tempDirName}/{repoName}_publish.tar.gz"), $"/tmp/{repoName}_publish.tar.gz");
                    sshClient.RunCommand($"tar zxf /tmp/{repoName}_publish.tar.gz --directory /tmp/{repoName}_publish").Execute();
                    sshClient.RunCommand($"cd /tmp/{repoName}_publish && dotnet restore").Execute();
                    sshClient.RunCommand($"cd /tmp/{repoName}_publish && dotnet build").Execute();
                    sshClient.RunCommand($"cd /tmp/{repoName}_publish && dotnet publish").Execute();
                    
                    //TODO Do the dockerfile build process. The files are on Gollum!
                    
                    scpClient.Disconnect();
                }                
                sshClient.Disconnect();
            }
        }
    }
}