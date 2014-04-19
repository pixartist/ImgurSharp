using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImgurSharp
{
    public class RequestParameters
    {
        private List<string> p;
        public RequestParameters()
        {
            p = new List<string>();
        }
        public void Add(string name, string value)
        {
            p.Add(name + "=" + value);

        }
        public void Add(string name)
        {
            p.Add(name);
        }
        public string this[string name]
        {
            set
            {
                Add(name, value);
            } 
        }
        public byte[] GetBytes()
        {
            return Encoding.ASCII.GetBytes(ToString());
        }
        public override string ToString()
        {
            return String.Join("&", p);
        }
        public void AddToRequest(System.Net.HttpWebRequest request)
        {
            byte[] data = GetBytes();
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
        }
    }
}
