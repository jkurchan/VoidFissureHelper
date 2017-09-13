using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tesseract.ConsoleDemo
{
    class WarframeItem
    {
        [JsonProperty("item_name")]
        public string Name { get; set; }
        [JsonProperty("item_type")]
        public string Type { get; set; }

        public WarframeItem() { }
    }
}
