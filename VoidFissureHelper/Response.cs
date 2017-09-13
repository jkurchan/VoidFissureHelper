using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tesseract.ConsoleDemo
{
    class Response
    {
        [JsonProperty("sell")]
        public List<User> Sellers { get; set; }

        [JsonProperty("buy")]
        public List<User> Buyers { get; set; }
    }
}
