using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage;
using System;
using System.IO;

public static class UploadFileFunction
{
    [FunctionName("UploadFile")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        string directoryName = req.Query["directoryName"];
        string fileName = req.Query["fileName"];
        string containerName = req.Query["containerName"];
        string accountName = req.Query["accountName"];
        string accountKey = req.Query["accountKey"];

        if (string.IsNullOrEmpty(directoryName) || string.IsNullOrEmpty(fileName) ||
            string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(accountKey))
        {
            return new BadRequestObjectResult("Please pass directoryName, fileName, containerName, accountName, and accountKey on the query string or in the request body");
        }

        try
        {
            var blobServiceEndpoint = $"https://{accountName}.blob.core.windows.net";
            var serviceClient = new BlobServiceClient(new Uri(blobServiceEndpoint), new StorageSharedKeyCredential(accountName, accountKey));
            var containerClient = serviceClient.GetBlobContainerClient(containerName);

            // Create the container if it does not exist
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Create a blob to represent the directory if it does not exist
            var directoryBlobClient = containerClient.GetBlobClient($"{directoryName}/");
            if (!await directoryBlobClient.ExistsAsync())
            {
                await directoryBlobClient.UploadAsync(new BinaryData(new byte[0]), overwrite: true);
            }

            // Upload file to the created directory
            var fileBlobClient = containerClient.GetBlobClient($"{directoryName}/{fileName}");
            using var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream);
            stream.Position = 0;
            await fileBlobClient.UploadAsync(stream, overwrite: true);

            return new OkObjectResult($"Uploaded file '{fileName}' to directory '{directoryName}' inside container '{containerName}'");
        }
        catch (Exception ex)
        {
            log.LogError($"An error occurred: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}




using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

public class BlobFolderClient
{
    private readonly string _loadBalancerUrl;

    public BlobFolderClient(string loadBalancerUrl)
    {
        _loadBalancerUrl = loadBalancerUrl;
    }

    public async Task CreateFolderAndUploadFileAsync(string directoryName, string fileName, string filePath, string containerName, string accountName, string accountKey)
    {
        using HttpClient client = new HttpClient();

        // Build the full URL with query parameters
        string requestUrl = $"{_loadBalancerUrl}?directoryName={directoryName}&fileName={fileName}&containerName={containerName}&accountName={accountName}&accountKey={accountKey}";

        try
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            // Send POST request
            HttpResponseMessage response = await client.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + result);
            }
            else
            {
                // Detailed error handling
                string errorDetails = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error: " + response.StatusCode);
                Console.WriteLine("Error Details: " + errorDetails);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("Request Exception: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }
    }
}



public class Program
{
    public static async Task Main(string[] args)
    {
        string loadBalancerUrl = "https://<your-load-balancer-url>";
        string directoryName = "my-directory";
        string fileName = "myfile.txt";
        string filePath = @"path\to\local\myfile.txt";
        string containerName = "<your-container-name>";
        string accountName = "<your-storage-account-name>";
        string accountKey = "<your-storage-account-key>";

        var client = new BlobFolderClient(loadBalancerUrl);
        await client.CreateFolderAndUploadFileAsync(directoryName, fileName, filePath, containerName, accountName, accountKey);
    }
}
