using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWSLambda2
{
    [DynamoDBTable("Music")]
    public class Music
    {
        public string Artist { get; set; }
        public string SongTitle { get; set; }
    }
}
