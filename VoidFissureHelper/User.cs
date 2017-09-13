using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tesseract.ConsoleDemo
{
    class User
    {
        [JsonProperty("online_status")]
        public bool IsOnline { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("online_ingame")]
        public bool IsIngame { get; set; }

        [JsonProperty("ingame_name")]
        public string Name { get; set; }
    }
}
