using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AWSLambda2
{
    class SecretDict
    {
        public string accessKey { get; set; }
        public string secretKey { get; set; }
        public string TableName { get; set; }

        public SecretDict(string json)
        {
            JObject jObject = JObject.Parse(json);
            accessKey = (string)jObject["AccountAccessKey"];
            secretKey = (string)jObject["AccountSecretKey"];
            TableName = (string)jObject["TableNameWorkWith"];
        }

    }
}
