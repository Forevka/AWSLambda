using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace AWSLambda2
{
    public class SongDescription
    {
        public string Artist { get; set; }
        public string SongTitle { get; set; }

        public string Duration { get; set; }

        public SongDescription(string json)
        {
            JObject jObject = JObject.Parse(json);
            Artist = (string)jObject["Artist"];
            SongTitle = (string)jObject["SongTitle"];
            Duration = (string)jObject["Duration"];
        }
    }
}
