using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Web;
using System.Net;
using System.IO;
namespace ImgurSharp
{
    public class Imgur
    {
        public const string UrlToken = "https://api.imgur.com/oauth2/token";
        public const string UrlAuth = "https://api.imgur.com/oauth2/authorize";
        public const string UrlSignin = "https://imgur.com/signin";
        public const string UrlUpload = "https://api.imgur.com/3/image/";
        public const string UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

        private string clientID;
        private string clientSecret;
        private string user;
        private string password;
        private string token = null;
        private CookieContainer cookies;

        public Imgur(string user, string password, string clientID, string clientSecret)
        {
            this.password = password;
            this.user = user;
            this.clientID = clientID;
            this.clientSecret = clientSecret;
            cookies = new CookieContainer();
        }
        public void CreateSession()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UrlSignin);
            HttpWebResponse response;

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "ext/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            
            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.Referer = "https://imgur.com/signin";
            request.CookieContainer = cookies;
            RequestParameters postData = new RequestParameters();
            postData["username"] = user;
            postData["password"] = password;
            postData["remember"] = "remember";
            postData.Add("submit");
            postData.AddToRequest(request);

            response = (HttpWebResponse)request.GetResponse();
            cookies.Add(response.Cookies);;
            
        }
        public string RequestPin()
        {
            string pin = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UrlAuth + "?client_id=" + clientID + "&response_type=pin");
            HttpWebResponse response;

            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "ext/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = true;
            request.CookieContainer = cookies;

            response = (HttpWebResponse)request.GetResponse();
            cookies.Add(response.Cookies);
            pin = response.ResponseUri.Query;
            if (!pin.Contains("?pin="))
                throw (new Exception("Response does not contain pin: " + response));
            pin = pin.Substring(pin.IndexOf("?pin=") + 5);
            if (pin.Contains("&"))
                pin = pin.Substring(0, pin.IndexOf("&"));
            return pin;
        }
        public string RequestToken(string pin)
        {
            string accessToken = null;
            //get token
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string template = "client_id={0}&client_secret={1}&grant_type=pin&pin={2}";
                string data = String.Format(template, clientID, clientSecret, pin);
                string resource = UrlToken;
                string response = null;
                try
                {
                    response = client.UploadString(resource, data);
                }
                catch (Exception e)
                {
                    throw (new Exception("Could not get token response", e));
                }

                int start = response.IndexOf("token\":\"") + 8;
                int end = response.Substring(start).IndexOf("\"");
                accessToken = response.Substring(start, end);
            }

            return accessToken;
        }
        public string UploadImage(string title, string description, System.Drawing.Image image, string token)
        {
            string base64 = ImageToBase64(image, System.Drawing.Imaging.ImageFormat.Png);

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Authorization", "Bearer " + token);

                NameValueCollection data = new NameValueCollection();
                data["image"] = base64;
                data["type"] = "base64";
                data["title"] = title;
                data["description"] = description;

                byte[] responsePayload = client.UploadValues(UrlUpload, "POST", data);
                string response = Encoding.ASCII.GetString(responsePayload);
                string start = "\"link\":\"";
                string end = "\"";
                int i = response.IndexOf(start);
                if(i < 0)
                    throw(new Exception("Could not find image link in response"));
                string url = response.Substring(i + start.Length);
                url = url.Substring(0, url.IndexOf(end));
                return url;
            }
        }
        public string ImageToBase64(System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }
    }
}
