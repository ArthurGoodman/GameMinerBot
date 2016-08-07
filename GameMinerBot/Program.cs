using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Serialization;

namespace GameMinerBot {
    public class Settings {
        public string UserAgent { get; set; }
        public string Xsrf { get; set; }
        public string Token { get; set; }

        public Settings() {
            UserAgent = "";
            Xsrf = "";
            Token = "";
        }
    }

    class Program {
        static string SettingsFileName { get { return "settings.xml"; } }

        static Settings settings;

        static void Main(string[] args) {
            LoadSettings();

            Run("coal");
            Run("sandbox");

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static void LoadSettings() {
            settings = new Settings();

            XmlSerializer s = new XmlSerializer(typeof(Settings));

            if (File.Exists(SettingsFileName)) {
                Stream stream = File.OpenRead(SettingsFileName);
                settings = (Settings)s.Deserialize(stream);
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(SettingsFileName);
            s.Serialize(writer, settings);
            writer.Close();
        }

        static void Run(string category) {
            Console.WriteLine("Category: " + category + "\n");

            for (int page = 1; true; page++) {
                Console.WriteLine("Page " + page);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(GetPage("http://gameminer.net/giveaway/" + category + "?type=&filter_entered=on&q=&sortby=finish&order=asc&page=" + page));

                var giveawayDivs = GetElementsByClass(doc.DocumentNode, "div", "c-giveaway");

                if (giveawayDivs.Count() == 0)
                    break;

                foreach (var div in giveawayDivs) {
                    Thread.Sleep(100);

                    var coal = GetElementsByClass(div, "span", "g-coal-icon");

                    if (coal.Count() > 0 && coal.First().InnerHtml == "0 coal") {
                        var giveawayName = GetElementsByClass(div, "a", "giveaway__name");

                        if (giveawayName.Count() > 0)
                            Console.Write(giveawayName.First().InnerHtml);

                        JoinGiveaway(div.Attributes["data-code"].Value);
                    }
                }
            }

            Console.WriteLine();
        }

        static IEnumerable<HtmlNode> GetElementsByClass(HtmlNode node, string name, string _class) {
            return node.Descendants(name).Where(n => n.Attributes.Contains("class") && n.Attributes["class"].Value.Contains(_class));
        }

        static string GetPage(string url) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.UserAgent = settings.UserAgent;

            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(request.RequestUri, new Cookie("_xsrf", settings.Xsrf));
            request.CookieContainer.Add(request.RequestUri, new Cookie("token", settings.Token));

            return new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
        }

        static void JoinGiveaway(string id) {
            using (WebClient client = new WebClient()) {
                client.Headers["User-Agent"] = settings.UserAgent;
                client.Headers.Add(HttpRequestHeader.Cookie, "_xsrf=" + settings.Xsrf + ";token=" + settings.Token);

                client.UploadValues("http://gameminer.net/giveaway/enter/" + id, new NameValueCollection() { { "_xsrf", settings.Xsrf } });
            }
        }
    }
}
