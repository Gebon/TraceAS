using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TraceAS
{
    class Program
    {
        private static void Main(string[] args)
        {
            const string formatString = "https://www.ultratools.com/tools/asnInfoResult?domainName={0}";
            const string regex = @"<div class=""tool-results-heading"">(?<asNumber>.+?)</div>";

            var processInfo = new ProcessStartInfo("tracert")
            {
                Arguments = args[0],
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var tasks = new List<Task>();

            using (var process = Process.Start(processInfo))
            using (var reader = process.StandardOutput)
            using (var client = new HttpClient())
                foreach (var line in ReadLines(reader).Where(line => line != ""))
                {
                    DoWork(line, formatString, client, regex);
                }
        }

        private static void DoWork(string line, string formatString, HttpClient client, string regex)
        {
            var sequenceNumber = 0;
            if (!int.TryParse(line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], out sequenceNumber))
                return;
            var ip = Regex.Match(line, @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})").Groups["ip"].Value;

            if (!IsCorrectIp(ip))
            {
                if (ip == "")
                {
                    Console.WriteLine("{0}: * * *", sequenceNumber);
                    return;
                }
                Console.WriteLine("{0}: {1}", sequenceNumber, ip);
                return;
            }
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), String.Format(formatString, ip)))
            {
                var response = client.SendAsync(request).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                {
                    Console.WriteLine("{0}: {1} {2}", sequenceNumber, ip, Regex.Match(content, regex).Groups["asNumber"].Value);
                }
            }
        }

        private static bool IsCorrectIp(string ip)
        {
            for (int i = 6; i < 10; i++)
            {
                if (ip.StartsWith(String.Format("172.1{0}.", i)))
                    return false;
            }

            for (int i = 0; i < 10; i++)
            {
                if (ip.StartsWith(String.Format("172.2{0}.", i)))
                    return false;
            }

            for (int i = 0; i < 2; i++)
            {
                if (ip.StartsWith(String.Format("172.3{0}.", i)))
                    return false;
            }
            return true;
        }

        private static IEnumerable<string> ReadLines(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
        }
    }
}
