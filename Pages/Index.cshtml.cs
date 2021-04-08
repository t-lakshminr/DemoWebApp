using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Diagnostics;
using System.Text;

namespace DemoWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }

        public static string ExecuteCurl(string curlCommand, int timeoutInSeconds = 60)
        {
            if (string.IsNullOrEmpty(curlCommand))
                return "";

            curlCommand = curlCommand.Trim();

            // remove the curl keworkd
            if (curlCommand.StartsWith("curl"))
            {
                curlCommand = curlCommand.Substring("curl".Length).Trim();
            }

            // this code only works on windows 10 or higher
            {

                curlCommand = curlCommand.Replace("--compressed", "");

                // windows 10 should contain this file
                var fullPath = System.IO.Path.Combine(Environment.SystemDirectory, "curl.exe");

                if (System.IO.File.Exists(fullPath) == false)
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    throw new Exception("Windows 10 or higher is required to run this application");
                }

                // on windows ' are not supported. For example: curl 'http://ublux.com' does not work and it needs to be replaced to curl "http://ublux.com"
                List<string> parameters = new List<string>();


                // separate parameters to escape quotes
                try
                {
                    Queue<char> q = new Queue<char>();

                    foreach (var c in curlCommand.ToCharArray())
                    {
                        q.Enqueue(c);
                    }

                    StringBuilder currentParameter = new StringBuilder();

                    void insertParameter()
                    {
                        var temp = currentParameter.ToString().Trim();
                        if (string.IsNullOrEmpty(temp) == false)
                        {
                            parameters.Add(temp);
                        }

                        currentParameter.Clear();
                    }

                    while (true)
                    {
                        if (q.Count == 0)
                        {
                            insertParameter();
                            break;
                        }

                        char x = q.Dequeue();

                        if (x == '\'')
                        {
                            insertParameter();

                            // add until we find last '
                            while (true)
                            {
                                x = q.Dequeue();

                                // if next 2 characetrs are \' 
                                if (x == '\\' && q.Count > 0 && q.Peek() == '\'')
                                {
                                    currentParameter.Append('\'');
                                    q.Dequeue();
                                    continue;
                                }

                                if (x == '\'')
                                {
                                    insertParameter();
                                    break;
                                }

                                currentParameter.Append(x);
                            }
                        }
                        else if (x == '"')
                        {
                            insertParameter();

                            // add until we find last "
                            while (true)
                            {
                                x = q.Dequeue();

                                // if next 2 characetrs are \"
                                if (x == '\\' && q.Count > 0 && q.Peek() == '"')
                                {
                                    currentParameter.Append('"');
                                    q.Dequeue();
                                    continue;
                                }

                                if (x == '"')
                                {
                                    insertParameter();
                                    break;
                                }

                                currentParameter.Append(x);
                            }
                        }
                        else
                        {
                            currentParameter.Append(x);
                        }
                    }
                }
                catch
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    throw new Exception("Invalid curl command");
                }

                StringBuilder finalCommand = new StringBuilder();

                foreach (var p in parameters)
                {
                    if (p.StartsWith("-"))
                    {
                        finalCommand.Append(p);
                        finalCommand.Append(" ");
                        continue;
                    }

                    var temp = p;

                    if (temp.Contains("\""))
                    {
                        temp = temp.Replace("\"", "\\\"");
                    }
                    if (temp.Contains("'"))
                    {
                        temp = temp.Replace("'", "\\'");
                    }

                    finalCommand.Append($"\"{temp}\"");
                    finalCommand.Append(" ");
                }


                using (var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "curl.exe",
                        Arguments = finalCommand.ToString(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.SystemDirectory
                    }
                })
                {
                    proc.Start();

                    proc.WaitForExit(timeoutInSeconds * 1000);

                    return proc.StandardOutput.ReadToEnd();
                }
            }
        }
    }
}
