/* 	Usage: Lambda function to add FlowEntity (See class at bottom) to Azure Table Storage
 * 
 *	Configuration notes:
 *	Trigger: HTTP (req)
 *	Inputs: Azure Table Storage (inTable)
 *	Outputs: HTTP ($return)
 * 		 Azure Table Storage (outputTable)
 *
 *	Responds to HTTP GET and POST
 * 	
 *	Request Parameter Name: req
 * 	Authorization Level: Function
 *
 */


#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

//using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Web;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log, CloudTable inTable, CloudTable outputTable)
{   
    // Determine which handler to run
    if (req.Method == "POST")
    {
        // The action is a POST.
        log.LogInformation("Handling Post Request with Data.");
        return await handlePOST(req, outputTable);
    } else {
        // The action is a GET.
        log.LogInformation("Handling GET Request. Responding with expected state.");
        return await handleGET(req);
    }

}

/* Updates entry in Azure Storage Table. Returns Success if successful */
private static async Task<IActionResult> handlePOST(HttpRequest req, CloudTable outputTable) {
    // What does this do?
    string device = req.Query["deviceID"];
    string softwareVersion = req.Query["softwareVersion"];
    string action = req.Query["Action"];
    string value = req.Query["Value"];
    
    // get request body
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);

    // Not sure what this is doing
    device = device ?? data?.deviceID;
    softwareVersion = softwareVersion ?? data?.softwareVersion;
    action = action ?? data?.Action;
    value = value ?? data?.Value;

    // Create a new customer entity.
    FlowEntity flowEntity = new FlowEntity(device);
    flowEntity.flow = value;

    // Create the TableOperation object that inserts the customer entity.
    TableOperation insertOperation = TableOperation.Insert(flowEntity);

    // Execute the insert operation.
    outputTable.ExecuteAsync(insertOperation);

    return (ActionResult)new OkObjectResult("Successfully added flow");
}

/* Responds with metadata for configuration of this device */
private static async Task<IActionResult> handleGET(HttpRequest req) {
    return (ActionResult)new OkObjectResult("{'version' : '0.1'}");
}

public class FlowEntity : TableEntity
{
    public FlowEntity(string deviceID)
    {
        this.PartitionKey = deviceID;
        this.RowKey = (DateTime.UtcNow).ToString("yyyyMMddHHmmssffff");
    }

    public string flow { get; set; }
}
