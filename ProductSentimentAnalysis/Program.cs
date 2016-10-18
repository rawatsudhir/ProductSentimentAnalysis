using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;


namespace ProductSentimentAnalysis
{
    class Program
    {
        public class getSentiment
        {
            public List<SentimentClassElement> documents { get; set; }
        }
        public class SentimentClassElement
        {
            public string language { get; set; }
            public string id { get; set; }
            public string text { get; set; }
        }

        static void Main(string[] args)
        {

            Console.WriteLine("Process Start.");
            string connectionString = "Server=tcp:XXXXX.database.windows.net,1433;Database=XXXXX;User ID=XXXXXXX;Password=XXXXXXX;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;";
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand();


            cmd.CommandText = "SELECT Tweet_id, [Tweet_Text] FROM [TweetFromTwitter] Where [Score] is NULL order by Tweet_id";
            cmd.CommandType = CommandType.Text;
            cmd.Connection = sqlConnection;

            sqlConnection.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                // Data is accessible through the DataReader object here.
                while (reader.Read())
                {
                    // Console.WriteLine(reader.GetValue(0).ToString());
                    //Console.WriteLine(reader.GetValue(1).ToString());
                    // Console.WriteLine(Convert.ToInt32(reader.GetValue(0).ToString()));
                    // Console.WriteLine(reader.GetValue(2).ToString());
                    Double SentimentValue;
                    try
                    {


                        var gSentiments = new getSentiment
                        {
                            documents = new List<SentimentClassElement>
                            {
                                new SentimentClassElement {language = "en", id = "1",text = reader.GetValue(1).ToString()},
                            }
                        };
                        var httprequestbody = JsonConvert.SerializeObject(gSentiments, Formatting.Indented);

                        string sentimenturi = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";
                        WebRequest request = WebRequest.Create(sentimenturi);
                        // Set the Method property of the request to POST.
                        request.Method = "POST";
                        // Set the ContentType property of the WebRequest.
                        request.ContentType = "application/json";
                        request.Headers.Add("Ocp-Apim-Subscription-Key:XXXXXXXXXXXXXXXXXXXXXXXXXXXXX"); //Create Cognitive service from Azure portal and add it key here
                        request.ContentLength = httprequestbody.Length;
                        using (var stream = new StreamWriter(request.GetRequestStream()))
                        {
                            stream.Write(httprequestbody);
                        }
                        WebResponse response = request.GetResponse();
                        Stream dataStream = response.GetResponseStream();
                        StreamReader responsereader = new StreamReader(dataStream);
                        string responseFromServer = responsereader.ReadToEnd();
                        JObject data = JObject.Parse(responseFromServer);
                        JToken Sentimentscore = data["documents"].First["score"];
                        SentimentValue = Convert.ToDouble(Sentimentscore);
                    }
                    catch (Exception e)
                    {
                        SentimentValue = 0; //IF there any error while getting sentiment score. This will help in torubleshoot the error with specific twwets
                    }
                    SqlConnection sqlConnection1 = new SqlConnection(connectionString);
                    sqlConnection1.Open();
                    SqlCommand sqlupdatecmd = new SqlCommand("Update [TweetFromTwitter] Set Score=@score Where Tweet_id=@tweet_id", sqlConnection1);
                    sqlupdatecmd.Parameters.Add("@score", SqlDbType.Float).Value = SentimentValue;
                    sqlupdatecmd.Parameters.Add("@tweet_id", SqlDbType.Int).Value = Convert.ToInt32(reader.GetValue(0).ToString());
                    sqlupdatecmd.ExecuteNonQuery();
                    sqlConnection1.Close();
                }
                reader.Close();

            }
            sqlConnection.Close();
            Console.WriteLine("Process End.");

        }
    }
}
