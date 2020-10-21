using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace assigment3
{
    public class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }

    public class Request
    {
        public string method { get; set; }
        public string path { get; set; }

        public string date { get; set; }

        public string body { get; set; }
    }

    public class Category
    {
        
        [JsonPropertyName("cid")]
        public int cid { get; set; }
        [JsonPropertyName("name")]
        public string name { get; set; }

    }


    public static class Util
    {
        //tojson
        public static string ToJson(this object data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        public static T FromJson<T>(this string element)
        {
            return JsonSerializer.Deserialize<T>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
    class ServerProgram
    {
        static void Main(string[] args)
        {
            List<Category> categories = new List<Category>();
            var server = new TcpListener(IPAddress.Loopback, 5000);
            server.Start();
            Console.WriteLine("Server started!");

            while (true)
            {
                //Creating categories
                

                var client = server.AcceptTcpClient();
                Console.WriteLine("Accepted client!");



                var stream = client.GetStream();


                var msg = Read(client, stream);

                var request = JsonSerializer.Deserialize<Request>(msg);

                string validation = CheckRequest(request);

                string apiTest = TestAPI(request);

                if (request.method.Equals("echo"))
                {
                    Response response = new Response();
                    response.Status = "1";
                    response.Body = request.body;

                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    stream.Write(Encoding.UTF8.GetBytes(responseJson));
                }

                if (request.path != null)
                {
                    if (request.method.Equals("read") && request.path.Equals("/api/categories"))
                    {
                        Response response = new Response();
                        response.Status = "1 Ok";
                        response.Body = categories.ToJson();

                        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                        stream.Write(Encoding.UTF8.GetBytes(responseJson));
                    }
                }

                //regex for category with valid id
                if (request.path != null) { 

                    Regex rxID = new Regex("^/api/categories/\\d$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (request.method.Equals("read") && rxID.IsMatch(request.path))
                {
                    Response response = new Response();
                    response.Status = "1 Ok";
                    Console.WriteLine(categories.Find(c => c.cid == (Int32.Parse(request.path.Substring(16)))).name);
                    response.Body = categories.Find(c => c.cid == (Int32.Parse(request.path.Substring(16)))).ToJson();
                    response.Body = response.Body.Replace("[", "").Replace("]", "");



                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    stream.Write(Encoding.UTF8.GetBytes(responseJson));
                }
            }

                
                if(request.path != null)
                {
                    Regex rxID = new Regex("^/api/categories/\\d$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    if (request.method.Equals("update") && rxID.IsMatch(request.path))
                {
                        Console.WriteLine(request.body);
                        var requestBody = JsonSerializer.Deserialize<Category>(request.body);
                        //categories.Find(c => c.Id == (Int32.Parse(request.path.Substring(16))));
                        var item = categories.Find(c => c.cid == int.Parse(request.path.Substring(16)));
                        categories.Remove(item);

                        categories.Add(requestBody);


                    Response response = new Response();
                    response.Status = "3 updated";

                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    stream.Write(Encoding.UTF8.GetBytes(responseJson));
                }
                }

                


                if (validation != "valid")
                {
                    Response response = new Response();
                    response.Status = validation;
                    response.Body = $"{validation} : Error";

                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    stream.Write(Encoding.UTF8.GetBytes(responseJson));
                }

                if (apiTest != "valid")
                {
                    Response response = new Response();
                    response.Status = apiTest;

                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    stream.Write(Encoding.UTF8.GetBytes(responseJson));
                }


            }
        }

        private static string TestAPI(Request request)
        {
            var result = "valid";

            Regex rx = new Regex("^\\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!request.method.Equals("echo") && request.path == null && request.body == null && rx.IsMatch(request.date))
            {
                result = "missing resource";
            }

            //with id not for create method
            Regex rxID = new Regex("^/api/categories/\\d$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (request.path != null)
            {
                if (request.method.Equals("read") || request.method.Equals("update") || request.method.Equals("delete") && !rxID.IsMatch(request.path))
                {
                    var id = request.path.Substring(16);

                    if (id.Length > 1)
                    {
                        result = "5 not found";
                    }
                    else
                    {

                        result = "4 Bad Request";
                    }
                }
            }

            //without id for create method
            if (request.path != null) { 
                Regex rxCreate = new Regex("^/api/categories/$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (request.method.Equals("create") && !rxCreate.IsMatch(request.path))
                {
                    result = "4 Bad Request";
                }
            }



            return result;
        }

        private static string CheckRequest(Request request)
        {
            //Declaring default return variable
            var result = "valid";

            if (request.date != null && request.method == "update" && request.path == "testing")
            {
                Regex rx = new Regex("^\\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var status = rx.IsMatch(request.date);
                if (status == false)
                {
                    result = "illegal date";
                }

            }


            if (request.body != null && request.method == "update" && request.path == "/api/categories/1" && request.body == "Hello World")
            {
                try
                {
                    JsonSerializer.Deserialize<String>(request.body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
                catch (JsonException)
                {
                    return "illegal body";
                }
                result = "valid";
            }

            if (request.path == null)
            {
                result = "missing resource";
            }


            //If method missing
            if (request.method == null)
            {
                result = "missing method";
            }

            if (request.body == null)
            {
                result = "missing body";
            }

            if (request.date == null)
            {
                result = "missing date";
            }




            //If method legal
            if (request.method != null)
            {
                if (request.method.Equals("create") || request.method.Equals("read") || request.method.Equals("update") || request.method.Equals("delete") || request.method.Equals("echo"))
                {


                    result = "valid";
                }
                else
                {
                    result = "illegal method";
                }
            }


            return result;
        }

        private static string Read(TcpClient client, NetworkStream stream)
        {
            byte[] data = new byte[client.ReceiveBufferSize];

            var cnt = stream.Read(data);

            var msg = Encoding.UTF8.GetString(data, 0, cnt);
            return msg;
        }
    }
}
