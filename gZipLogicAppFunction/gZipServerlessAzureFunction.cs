//#############################################################
//This Function is generic to process incoming JSON content to compress or decompress. 
//This Function is written to process GZIP compression/decompression logic for incoming HTTP Request containing a JSON. 
//It can be called in Logic App or in any other App. 
//-----------------------------------------------------------
//DECOMPRESSION Specifications/Requirements for PayLoad. 
//-------------------------------------------------------------
//Decompress request must meet expected JSON structure: content-encoding and content fields. {content-encoding:"gzip", content="whatevervalue"}
//incoming compressed string must be Base64 string. Otherwise it will fail to decompress.
//----------------------------
//COMPRESSION Specs/Requirements
//----------------------------
//Incoming HTTP Request.Body will be compressed. It can be any type of data. No JSON requirement. 
//outgoing compressed operation will also be returned as Base64 string 
//compress request could be just any request body. 
//############################################################
//Developer : Moonis Tahir : 5/30/2021
//Last Update : 6/2/2021
//#############################################################

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Text;

namespace gZipServerlessAzureFunction
{
    public static class gZipServerlessAzureFunction
    {
        private static string isBase64Str = "true";

        [FunctionName("gZipServerlessAzureFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string responseMessage = "";
            HttpRequest  copyRequest = req;

            req.Headers.TryGetValue("Content-Encoding", out var contentEncodingValue); //if coming in Header
            //try
            //{
            //    //When gZip file will arrive, simply decompress and return the results. 
            //    //    if (req.Body.GetType() == typeof(Microsoft.AspNetCore.WebUtilities.FileBufferingReadStream))
            //    var gZipFile = new MemoryStream();
            //    req.Body.CopyTo(gZipFile);
            //    responseMessage = DecompressGZipFile(gZipFile.ToArray());
            //    return new OkObjectResult(responseMessage);

            //}
            //catch (Exception ex) { Console.WriteLine(ex.Message); }
            //finally
            //{
            //    Console.WriteLine("Moving to string compress/decompress processing section");
            //}

            var requestBody = await new StreamReader(copyRequest.Body).ReadToEndAsync(); //Request Body is a file stream object. Must convert to proper string value
            string GZipEncoding = "gzip";
           // string DeflateEncoding = "deflate"; //Not implemented yet. Can be easily added later. 

            string gzipAction = req.Query["gzipAction"]; //GET Query String for gzip compress or decompress
             isBase64Str = req.Query["isBase64Str"];

            //This only applies to Decompress
            var incominggZipContent = "";
            
            
            if (contentEncodingValue.Equals(GZipEncoding))
            {
                if (gzipAction.Contains("decompress"))
                {
                    //Extract Request Body as JSON , incoming HTTP request must be JSON with base64 in content field. Expected must to have fields are content, content-encoding
                    var incomingJSONBody = JObject.Parse(requestBody);
                    if (string.IsNullOrEmpty(contentEncodingValue)) { contentEncodingValue = incomingJSONBody["content-encoding"].ToString(); } //if coming in body JSON

                    //some senders were sending as content field or just $content field. 
                    if (incomingJSONBody["content"]!=null)
                        incominggZipContent = incomingJSONBody["content"].ToString(); //extract the incoming content value
                    else if (incomingJSONBody["$content"] != null)
                        incominggZipContent = incomingJSONBody["$content"].ToString(); //extract the incoming content value

                    responseMessage = DecompressGZipString(incominggZipContent);
                }
                else if (gzipAction.Contains("compress"))
                {
                    responseMessage = CompressGZipString(requestBody);
                }
            }
            else //any other content encoding, simply return AS-IS. 
            {
                responseMessage = requestBody;
            }

             responseMessage = string.IsNullOrEmpty(gzipAction)
                ? "This HTTP triggered function executed successfully. Pass a gzipAction in the query string."
                : $"{responseMessage}";

            return new OkObjectResult(responseMessage);
        }

        //Just expecting the consumer to pass isBase64Str in query string parameter. 
        //You can implement your own code to detect base64 for incoming request.Body. 
        private static bool isBase64Valid()
        {
            bool isValidResult = false;
            if (isBase64Str == null)
                isBase64Str = "false";

            if (isBase64Str.ToUpper() == "TRUE")
                isValidResult = true;

            return isValidResult;
        }
        /// <summary>
        /// Decompresses Gzip stream and returns string content.
        /// </summary>
        private static string DecompressGZipString(string contentData)
        {
            string responseData = "";

            //Stream stream;
            try
            {
                byte[] base64ToBytesConvertData;
                Console.WriteLine("Compressed data Input string : " + contentData + " and isBase64Str : " + isBase64Str);
                if (isBase64Valid())
                {base64ToBytesConvertData = Convert.FromBase64String(contentData); }
                else
                { base64ToBytesConvertData = ASCIIEncoding.UTF8.GetBytes(contentData); }

                //base64ToBytesConvertData = System.IO.File.ReadAllBytes(@"C:\Users\motahir\Downloads\wpc_109E7F_2041_20210601_B2EE0000E260AD60_1.json.gz");
                byte[] decompressedBytesData = gZipDecompressAndReturnArray(base64ToBytesConvertData);
                string decompressedStringData = ASCIIEncoding.ASCII.GetString(decompressedBytesData);
                responseData = decompressedStringData;
                Console.WriteLine("Compressed bytes data size: {0}", base64ToBytesConvertData.Length);
                Console.WriteLine("Decompressed bytes data size: {0}", decompressedBytesData.Length);
                Console.WriteLine("Decompressed Value:       {0}", decompressedStringData);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.StackTrace);
                throw ex;
            }
            finally
            {
                Console.WriteLine("Decompressed operation completed successfully.");
            }


            return responseData; 
        }

        private static string DecompressGZipFile(Byte[] gZipFileData)
        {
            string responseData = "";

            //Stream stream;
            try
            {

                //base64ToBytesConvertData = System.IO.File.ReadAllBytes(@"C:\Users\motahir\Downloads\wpc_109E7F_2041_20210601_B2EE0000E260AD60_1.json.gz");
                byte[] decompressedBytesData = gZipDecompressAndReturnArray(gZipFileData);
                string decompressedStringData = ASCIIEncoding.ASCII.GetString(decompressedBytesData);
                responseData = decompressedStringData;
                Console.WriteLine("Decompressed bytes data size: {0}", decompressedBytesData.Length);
                Console.WriteLine("Decompressed Value:       {0}", decompressedStringData);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.StackTrace);
                throw ex;
            }
            finally
            {
                Console.WriteLine("Decompressed operation completed successfully.");
            }


            return responseData;
        }

        /// <summary>
        /// Decompresses Gzip stream and returns string content.
        /// </summary>
        private static string CompressGZipString(string contentData)
        {
            string responseData = "";

            //Stream stream;
            try
            {
                Console.WriteLine("original data  : {0}", contentData);
                byte[] TextBytesConvertData = Encoding.ASCII.GetBytes(contentData);
                byte[] compressedBytesData = gZipCompressAndReturnArray(TextBytesConvertData);
                string CompressedStringData = System.Text.ASCIIEncoding.ASCII.GetString(compressedBytesData);
                responseData = Convert.ToBase64String(compressedBytesData);
                Console.WriteLine("Dompressed bytes data size: {0}", TextBytesConvertData.Length);
                Console.WriteLine("Compressed bytes data size: {0}", compressedBytesData.Length);
                Console.WriteLine("Compressed Value:       {0}", responseData);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.StackTrace);
                throw ex;
            }
            finally
            {
                Console.WriteLine("Compressed operation completed successfully.");
            }


            return responseData;
        }
  

        static byte[] gZipDecompressAndReturnArray(byte[] gzipData)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzipData),
                CompressionMode.Decompress))
            {

                using (var memoryStream = new MemoryStream()) {
                    stream.CopyTo(memoryStream); return memoryStream.ToArray();
                }
            }
        }

        static byte[] gZipCompressAndReturnArray(byte[] gzipData)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.

            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(gzipData, 0, gzipData.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        
    }
}
