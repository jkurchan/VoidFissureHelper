using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tesseract.ConsoleDemo
{
    class ItemResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("response")]
        public Response Reponse { get; set; }
    }
}
