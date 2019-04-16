using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Newtonsoft.Json;
using Amazon.Lambda.Core;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambda2
{
    public class Function
    {
        static readonly string accessKey = "AKIA6HXI6PYSO4UIOOES";
        static readonly string secretKey = "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";
        static readonly string tableName = "Music";

        static readonly BasicAWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        static readonly AmazonDynamoDBClient client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        static readonly DynamoDBContext worker = new DynamoDBContext(client);

        public async Task<string> FunctionHandler(FunctionInput input, ILambdaContext context)
        {
            Response r = new Response();
            if (input.Action == "write")
            {
                r.Status = MainWriteAsync(input.Value).Result;
                switch(r.Status)
                {
                    case 200:
                        r.Description = "OK";
                        break;
                    case 500:
                        r.Description = "TABLE DOESN`T EXIST";
                        break;
                    case 501:
                        r.Description = "VALUE CAN`T BE EMPTY STRING";
                        break;
                    case 502:
                        r.Description = "NOT ENOUGH ARGUMENTS";
                        break;
                }
                return JsonConvert.SerializeObject(r);
            }
            else if (input.Action == "read")
            {
                r.Status = 200;
                r.Description = JsonConvert.SerializeObject(MainReadAsync(input.Value).Result);
                return JsonConvert.SerializeObject(r);
            }
            r.Status = 510;
            r.Description = "UNKNOWN METHOD";
            return JsonConvert.SerializeObject(r);
        }

        public async static Task<int> MainWriteAsync(SongDescription Song)
        {
            ListTablesResponse tableResponse = await client.ListTablesAsync();
            if (Song != null)
            {
                Console.WriteLine(Song.Artist);
                if (Song.Artist != null && Song.SongTitle != null)
                {
                    if (Song.Artist != "" && Song.SongTitle != "")
                    {
                        if (tableResponse.TableNames.Contains(tableName))
                        {
                            Music currentMusic = new Music
                            {
                                Artist = Song.Artist,
                                SongTitle = Song.SongTitle
                            };

                            await worker.SaveAsync(currentMusic);
                            return 200;
                        }
                        else
                        {
                            return 500;
                        }
                    }
                    else return 501;
                }
            }
            return 502;
        }

        public async static Task<List<Music>> MainReadAsync(SongDescription SearchSong)
        {
            List<ScanCondition> conditions = new List<ScanCondition>();
            if (SearchSong != null)
            {
                if (SearchSong.Artist != null && SearchSong.Artist != "")
                {
                    conditions.Add(new ScanCondition("Artist", ScanOperator.Equal, SearchSong.Artist));
                }

                if (SearchSong.SongTitle != null && SearchSong.SongTitle != "")
                {
                    conditions.Add(new ScanCondition("SongTitle", ScanOperator.Equal, SearchSong.SongTitle));
                }
            }
            List<Music> allDocs = await worker.ScanAsync<Music>(conditions).GetRemainingAsync();

            return allDocs;
        }
    }


}
