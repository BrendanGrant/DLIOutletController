using DLIOutletController.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DLIOutletController
{
    public class DLIOutletClient
    {
        ConnectionInfo connectionInfo;
        CookieContainer cookies { get; set; }
        public DLIOutletClient(ConnectionInfo connectionInfo)
        {
            this.connectionInfo = connectionInfo;
            cookies = new CookieContainer();
        }


        private static HttpClient GetClient(CookieContainer cookies)
        {
            var handler = new HttpClientHandler() { CookieContainer = cookies, UseCookies = true };
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("User-Agent", "DLIOutletClient");
            client.DefaultRequestHeaders.Add("Accept", "text/html, application/xhtml+xml, image/jxr, */*");
            client.DefaultRequestHeaders.Add("Pragma", "no-cache");

            return client;
        }

        public async Task CycleOutlet(int outletIndex)
        {
            var client = GetClient(cookies);

            var url = GetUrl(connectionInfo, string.Format(CultureInfo.InvariantCulture, URLs.CycleN, outletIndex.ToString()));

            var body = await client.GetStringSafeAsync(url);

        }

        public async Task SetOutlet(int outletIndex, bool desiredState)
        {
            var client = GetClient(cookies);

            var url = GetUrl(connectionInfo, string.Format(CultureInfo.InvariantCulture, URLs.OutletNToState, outletIndex.ToString(), desiredState ? "ON" : "OFF"));

            var body = await client.GetStringSafeAsync(url);
        }

        public async Task<SwitchInfo> ConnectAndGetSwitchInfoAsync()
        {
            await ConnectAsync();
            return await GetSwitchInfo();
        }


        public async Task ConnectAsync()
        {
            var client = GetClient(cookies);


            Uri loginUrl = null;
            try
            {
                var body = await client.GetStringSafeAsync(GetUrl(connectionInfo));
                var h = new HtmlDocument();
                h.LoadHtml(body);

                var challengeNode = FindNode(h.DocumentNode, "name", "Challenge");
                var challenge = challengeNode.Attributes["value"].Value;

                var username = connectionInfo.Username;
                var password = challenge + connectionInfo.Username + connectionInfo.Password + challenge;
                
                var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(password.ToByteArray());

                var sb = new StringBuilder();
                for (int x = 0; x < 16; x++)
                {
                    sb.Append(hash[x].ToString("x2"));
                }
                password = sb.ToString();

                var content = new FormUrlEncodedContent(new[]
                {
                   new KeyValuePair<string, string>("Username", username),
                   new KeyValuePair<string, string>("Password", password),
                });


                loginUrl = GetUrl(connectionInfo, URLs.Login);
                var result = await client.PostAsync(loginUrl, content);

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to connect. Response code: {result.StatusCode}");
                }
                var responseBody = await result.Content.GetStringSafeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public async Task<SwitchInfo> GetSwitchInfo()
        {
            var client = GetClient(cookies);

            var responseBody = await client.GetStringSafeAsync(GetUrl(connectionInfo, URLs.Index));

            try
            {
                var switchInfo = new SwitchInfo();

                var controllerName = ParseControllerName(responseBody);
                var newOutlets = ParseRelayName(responseBody);

                switchInfo.Name = controllerName;
                switchInfo.Outlets = new OutletInfo[newOutlets.Count];
                int index = 0;
                foreach (var item in newOutlets)
                {
                    switchInfo.Outlets[index++] = new OutletInfo() { Index = item.Index, Name = item.Name, IsOn = item.IsOn };
                }

                return switchInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


            return null;
        }

        private static IList<OutletInfo> ParseRelayName(string responseBody)
        {
            var d = new HtmlAgilityPack.HtmlDocument();
            d.LoadHtml(responseBody);

            var node = FindNode(d.DocumentNode, "href", "outlet?");
            var altNode = d.DocumentNode.SelectSingleNode("/html[1]/body[1]/table[1]/tr[2]"); //2-?

            var outlets = new List<OutletInfo>();
            for (int x = 0; x < 8; x++)
            {
                var outletNumber = d.DocumentNode.SelectSingleNode(string.Format("/html[1]/body[1]/font[1]/table[1]/tr[1]/td[2]/table[2]/tr[{0}]/td[1]", x + 3));
                var outletName = d.DocumentNode.SelectSingleNode(string.Format("/html[1]/body[1]/font[1]/table[1]/tr[1]/td[2]/table[2]/tr[{0}]/td[2]", x + 3));
                var outletState = d.DocumentNode.SelectSingleNode(string.Format("/html[1]/body[1]/font[1]/table[1]/tr[1]/td[2]/table[2]/tr[{0}]/td[3]", x + 3));


                if (outletName == null || outletState == null)
                {
                    outletNumber = d.DocumentNode.SelectSingleNode(string.Format("/html[1]/body[1]/table[1]/tr[{0}]/td[1]", x + 2));
                    outletName = d.DocumentNode.SelectSingleNode(string.Format("/html[1]/body[1]/table[1]/tr[{0}]/td[2]", x + 2));
                    outletState = d.DocumentNode.SelectSingleNode(string.Format("/html[1]/body[1]/table[1]/tr[{0}]/td[3]", x + 2));
                }


                if ((outletName != null || outletState != null)
                    && (outletNumber.InnerText.Trim() != "Logout" && outletName.InnerText.Trim() != "Help"))
                {
                    try
                    {
                        outlets.Add(new OutletInfo() { Index = int.Parse(outletNumber.InnerText.Trim()), Name = outletName.InnerText.Trim(), IsOn = IsSwitchOn(outletState.InnerText) });

                    }
                    catch (Exception ex)
                    {
                        if(System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Diagnostics.Debug.WriteLine(ex);
                            System.Diagnostics.Debugger.Break();
                        }
                    }
                }
            }

            return outlets;
        }

        private static string ParseControllerName(string responseBody)
        {
            var targetString = "Controller:";
            var start = responseBody.IndexOf(targetString);

            if (start > 0)
            {
                var stop = responseBody.IndexOf("<", start);

                var name = responseBody.Substring(start + targetString.Length, stop - (start + targetString.Length));

                return name.Trim();
            }
            else
            {
                targetString = "<title>";
                start = responseBody.IndexOf(targetString);
                var stop = responseBody.IndexOf("</title>", start);

                var name = responseBody.Substring(start + targetString.Length, stop - (start + targetString.Length));

                return name.Trim();
            }
        }


        private static bool IsSwitchOn(string innerText)
        {
            return !innerText.Contains("OFF");
        }

        private static Uri GetUrl(ConnectionInfo connectionInfo)
        {
            if (connectionInfo.Port != 0)
            {
                return GetUrl(string.Format("{0}:{1}", connectionInfo.IPAddress, connectionInfo.Port), string.Empty);
            }
            else
            {
                return GetUrl(connectionInfo.IPAddress, string.Empty);
            }
        }

        private static Uri GetUrl(ConnectionInfo connectionInfo, string uri)
        {
            if (connectionInfo.Port != 0)
            {
                return GetUrl(string.Format("{0}:{1}", connectionInfo.IPAddress, connectionInfo.Port), uri);
            }
            else
            {
                return GetUrl(connectionInfo.IPAddress, uri);
            }
        }


        private static Uri GetUrl(string ip)
        {
            return GetUrl(ip, string.Empty);
        }

        private static Uri GetUrl(string ip, string uri)
        {
            string s = string.Format("http://{0}/{1}", ip, uri);
            return new Uri(s);
        }


        private static HtmlNode FindNode(HtmlNode h, string attributeName, string value)
        {
            try
            {
                if (h.Attributes[attributeName] != null && h.Attributes[attributeName].Value.Contains(value))
                {
                    return h;
                }

                foreach (var item in h.ChildNodes)
                {
                    var foo = FindNode(item, attributeName, value);

                    if (foo != null)
                        return foo;
                }
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    System.Diagnostics.Debugger.Break();
                }
            }
            return null;
        }
    }
}