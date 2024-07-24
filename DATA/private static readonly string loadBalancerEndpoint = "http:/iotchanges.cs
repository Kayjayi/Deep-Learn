private static readonly string loadBalancerEndpoint = "http://<your-load-balancer-ip>";



private static async Task SendDataToLoadBalancerAsync(string data)
{
    using (HttpClient client = new HttpClient())
    {
        var content = new StringContent(data, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(loadBalancerEndpoint, content);
        response.EnsureSuccessStatusCode();
        Console.WriteLine("Data sent to load balancer successfully.");
    }
}




private static async void ThreadBody(object userContext)
{
    var client = userContext as ModuleClient;

    if (client == null)
    {
        throw new InvalidOperationException("UserContext doesn't contain expected values");
    }

    while (true)
    {
        try
        {                    
            var files = Directory.GetFiles("exchange", SearchPattern);

            System.Console.WriteLine($"{DateTime.UtcNow} - Seen {files.Length} files with pattern '{SearchPattern}'");

            if (files.Length > 0)
            {
                foreach (var fileName in files)
                {
                    _lineNumber = 0;

                    try
                    {
                        var canRead = false;
                        var canWrite = false;

                        using (var fs = new FileStream(fileName, FileMode.Open))
                        {
                            canRead = fs.CanRead;
                            canWrite = fs.CanWrite;
                        }

                        if (!canRead && !canWrite)
                        {
                            System.Console.WriteLine($"File {fileName}: {(canRead ? "is readable" : "is not readable")}; {(canWrite ? "is writable" : "is not writable")}");                                                
                            continue;   
                        }
                    }
                    catch
                    {
                        System.Console.WriteLine($"{fileName} cannot be opened");
                        continue;
                    }

                    var fileInfo = new FileInfo(fileName);

                    System.Console.WriteLine($"File found: '{fileInfo.FullName}' - Size: {fileInfo.Length} bytes.");

                    if (fileInfo.Length == 0)
                    {
                        System.Console.WriteLine($"'{fileName}' is empty.");
                        continue;
                    }

                    var lines = File.ReadAllLines(fileName);

                    var i = 0;    
                    var count = 0;
                    string[] headers = new string[] { };

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            System.Console.WriteLine($"Ignored empty line {i + 1}");
                        }
                        else
                        {
                            if (i == 0)
                            {
                                headers = line.Split(Delimiter);
                            }
                            else
                            {
                                var expando = new ExpandoObject() as IDictionary<string, Object>;              
                                var values = line.Split(Delimiter);
                                var j = 0;

                                foreach (var header in headers)
                                {
                                    expando.Add(header, values[j]);
                                    j++;
                                }

                                _lineNumber++;
                                expando.Add("line", _lineNumber);
                                expando.Add("fileName", fileInfo.Name);
                                expando.Add("timestamp", DateTime.UtcNow);
                                expando.Add("moduleId", _moduleId);
                                expando.Add("deviceId", _deviceId);

                                var jsonMessage = JsonConvert.SerializeObject(expando);
                                await SendDataToLoadBalancerAsync(jsonMessage);

                                count++;    
                                Console.WriteLine($"Message {count} sent");
                            }

                            i++;
                            Thread.Sleep(LineDelay);
                        }
                    }

                    System.Console.WriteLine($"Processed {count} lines out of {lines.Length - 1} lines found in the file");

                    var fi = new FileInfo(fileName);
                    var targetFullFilename = fi.FullName + RenameExtension;
                    File.Move(fi.FullName, targetFullFilename);
                    System.Console.WriteLine($"Renamed '{fi.FullName}' to '{targetFullFilename}'");
                }
            }
        }
        catch (System.Exception ex)
        {  
            System.Console.WriteLine($"Exception: {ex.Message}");
        }

        var fileMessage = new FileMessage
        {
            counter = DateTime.Now.Millisecond
        };

        Thread.Sleep(Interval);
    }
}
