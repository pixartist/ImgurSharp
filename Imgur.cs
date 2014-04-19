using System;
using System.Web.Script.Serialization;
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

        private JavaScriptSerializer serializer;
        private string clientID;
        private string clientSecret;
        private string user;
        private string password;
        private CookieContainer cookies;
        public struct TokenInfo
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
            public string scope { get; set; }
            public string refresh_token { get; set; }
        }
        public struct ImageInfoContainer
        {
            public ImageInfo data {get; set;}
            public bool success { get; set; }
            public int status { get; set; }
        }
        public struct ImageInfo
        {
            public string id {get; set;}
            public string title { get; set; }
            public string description {get; set;}
            public string datetime {get; set;}
            public string type {get; set;}
            public string animated {get; set;}
            public int width {get; set;}
            public int height {get; set;}
            public int size {get; set;}
            public int views {get; set;}
            public int bandwidth {get; set;}
            public bool favourite {get; set;}
            public string nsfw {get; set;}
            public string section {get; set;}
            public string deletehash {get; set;}
            public string link {get; set;}
        }
        public Imgur(string user, string password, string clientID, string clientSecret)
        {
            this.password = password;
            this.user = user;
            this.clientID = clientID;
            this.clientSecret = clientSecret;
            this.cookies = new CookieContainer();
            this.serializer = new JavaScriptSerializer();
        }
        public void CreateSession()
        {
            Console.WriteLine("Creating session");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UrlSignin);
            HttpWebResponse response;

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "ext/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            
            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.Referer = UrlSignin;
            request.CookieContainer = cookies;
            RequestParameters postData = new RequestParameters();
            postData["username"] = user;
            postData["password"] = password;
            postData["remember"] = "remember";
            postData.Add("submit");
            postData.AddToRequest(request);

            response = (HttpWebResponse)request.GetResponse();
            cookies.Add(response.Cookies);;
            Console.WriteLine("Created session");
        }
        public string RequestPin()
        {
            Console.WriteLine("Requesting pin");
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
            Console.WriteLine("Recieved pin: " + pin);
            return pin;
        }
        public TokenInfo RequestToken(string pin)
        {
            Console.WriteLine("Requesting token");
            TokenInfo info;
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
                info = serializer.Deserialize<TokenInfo>(response);
                
            }
            Console.WriteLine("Recieved token: " + info.access_token);
            return info;
        }
        public ImageInfoContainer UploadImage(string title, string description, System.Drawing.Image image, string token)
        {
            ImageInfoContainer info = new ImageInfoContainer { success = false };
            Console.WriteLine("Uploading image");
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

                info = serializer.Deserialize<ImageInfoContainer>(response);
                
                
            }
            Console.WriteLine("Uploaded image: " + info.success);
            return info;
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
