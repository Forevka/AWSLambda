using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.IO;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda;
using Amazon.Lambda.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambda2
{
    public class Function
    {
        static string tableName;

        static BasicAWSCredentials credentials;
        static AmazonDynamoDBClient client;
        public static AmazonLambdaClient LambdaClient;
        static DynamoDBContext worker;

        public void InitialiseSecrets()
        {
            string secretName = "prod/DynamoLambda/SecretKeys";
            string region = "us-east-1";
            SecretDict secret;

            MemoryStream memoryStream = new MemoryStream();

            IAmazonSecretsManager clientSecret = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT"
            };
            GetSecretValueResponse response;
            try
            {
                response = clientSecret.GetSecretValueAsync(request).Result;
            }
            catch (Exception)
            {
                throw;
            }

            if (response.SecretString != null)
            {
                secret = new SecretDict(response.SecretString);

                tableName = secret.TableName;
                credentials = new BasicAWSCredentials(secret.accessKey, secret.secretKey);
                client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
                worker = new DynamoDBContext(client);
                LambdaClient = new AmazonLambdaClient(secret.accessKey, secret.secretKey, RegionEndpoint.USEast1);
            }
            else
            {
                memoryStream = response.SecretBinary;
                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
                secret = new SecretDict(decodedBinarySecret);

                tableName = secret.TableName;
                credentials = new BasicAWSCredentials(secret.accessKey, secret.secretKey);
                client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
                worker = new DynamoDBContext(client);
                LambdaClient = new AmazonLambdaClient(secret.accessKey, secret.secretKey, RegionEndpoint.USEast1);
            }
        }
        private static string Dump(object o)
        {
            string json = JsonConvert.SerializeObject(o, Formatting.Indented);
            return json;
        }
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            InitialiseSecrets();
            //Response r = new Response();
            var description = "UNKNOWN PATH";
            Console.WriteLine(input);

            SongDescription song = new SongDescription(input.Body);

            if (input.Path == "/song")
            {
                if (input.HttpMethod == "POST")
                {
                    var status = await MainWriteAsync(song);

                    switch (status)
                    {
                        case 200:
                            description = "OK";
                            break;
                        case 500:
                            description = "TABLE DOESN`T EXIST";
                            break;
                        case 501:
                            description = "VALUE CAN`T BE EMPTY STRING";
                            break;
                        case 502:
                            description = "NOT ENOUGH ARGUMENTS";
                            break;
                    }
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = status,
                        Body = description
                    };
                }
                else if (input.HttpMethod == "GET")
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 200,
                        Body = JsonConvert.SerializeObject(await MainReadAsync(song))
                    };
                }
                else if (input.HttpMethod == "PUT")
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 200,
                        Body = JsonConvert.SerializeObject(await MainUpdateAsync(song))
                    };
                }
                else if (input.HttpMethod == "DELETE")
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = await MainDeleteAsync(song),
                        Body = "Deleted"
                    };
                }
            }
            else description = "UNKNOWN METHOD";
            //r.Description = Dump(input) + Dump(context); 
            return new APIGatewayProxyResponse
            {
                StatusCode = 510,
                Body = description
            };
        }

        public async static Task<int> MainWriteAsync(SongDescription Song)
        {
            ListTablesResponse tableResponse = await client.ListTablesAsync();
            if (Song != null)
            {
                if (Song.Duration == null)
                    Song.Duration = " ";
                if (Song.Artist != null && Song.SongTitle != null)
                {

                    Song.SongTitle = await GetNormaliseText(Song.SongTitle);
                    Song.Artist = await GetNormaliseText(Song.Artist);
                    if (Song.Artist != "" && Song.SongTitle != "")
                    {
                        if (tableResponse.TableNames.Contains(tableName))
                        {
                            Music currentMusic = new Music
                            {
                                Artist = Song.Artist,
                                SongTitle = Song.SongTitle,
                                Duration = Song.Duration
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

        public async static Task<int> MainDeleteAsync(SongDescription DeleteSong)
        {
            Dictionary<string, AttributeValue> conditions = new Dictionary<string, AttributeValue>();
            if (DeleteSong != null)
            {
                if (DeleteSong.Artist != null && DeleteSong.Artist != "")
                {
                    conditions.Add("Artist", new AttributeValue { S = DeleteSong.Artist });
                }

                if (DeleteSong.SongTitle != null && DeleteSong.SongTitle != "")
                {
                    conditions.Add("SongTitle", new AttributeValue { S = DeleteSong.SongTitle });
                }
            }
            DeleteItemRequest request = new DeleteItemRequest
            {
                TableName = tableName,
                Key = conditions
            };

            await client.DeleteItemAsync(request);

            return 200;
        }
        public async static Task<UpdateItemResponse> MainUpdateAsync(SongDescription UpdateSong)
        {
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
            {
                { "Artist", new AttributeValue { S = UpdateSong.Artist } },
                { "SongTitle", new AttributeValue { S = UpdateSong.SongTitle } },
            };
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Duration"] = new AttributeValueUpdate()
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue { S = UpdateSong.Duration }
            };
            var request = new UpdateItemRequest
            {
                TableName = tableName,
                Key = key,
                AttributeUpdates = updates,
                ReturnValues = "UPDATED_NEW"
            };

            UpdateItemResponse r = await client.UpdateItemAsync(request);

            return r;
        }

        public async static Task<string> GetNormaliseText(string text)
        {
            text = "\"" + text + "\"";
            InvokeRequest ri = new InvokeRequest()
            {
                FunctionName = "NormaliseSongTitle",
                Payload = text,
                InvocationType = InvocationType.RequestResponse
            };
            InvokeResponse response = await LambdaClient.InvokeAsync(ri);
            var sr = new StreamReader(response.Payload);
            JsonReader reader = new JsonTextReader(sr);

            var serilizer = new JsonSerializer();
            var op = serilizer.Deserialize(reader);
            return op.ToString();
        }
    }


}
