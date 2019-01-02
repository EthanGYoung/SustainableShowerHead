/*      Usage: Lambda function to add new user (See class at bottom) to Azure Table Storage
 *
 *      Configuration notes:
 *      Trigger: HTTP (req)
 *      Inputs: Azure Table Storage (inputTable)
 *      Outputs: HTTP ($return)
 *               Azure Table Storage (outputTable)
 *
 *      Responds to HTTP GET and POST
 *
 *      Request Parameter Name: req
 *      Authorization Level: Function
 *
 */


#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;


/*
    "username": "user",
    "deviceID": "device_id_to_add",
*/
public static async Task<IActionResult> Run(HttpRequest req, ILogger log, CloudTable outputTable)
{
    log.LogInformation("Handling Post Request with New User.");

    // What does this do?
    string username = req.Query["username"];
    string device = req.Query["deviceID"];
    
    // get request body
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);

    // Not sure what this is doing
    username = username ?? data?.username;
    device = device ?? data?.deviceID;

    // Create a new customer entity.
    CustomerEntity custEntity = new CustomerEntity(device);
    custEntity.username = username;

    // Create the TableOperation object that inserts the customer entity.
    TableOperation insertOperation = TableOperation.Insert(custEntity);

    // Execute the insert operation.
    outputTable.ExecuteAsync(insertOperation);

    return (ActionResult)new OkObjectResult("Successfully added user");
}

public class CustomerEntity : TableEntity
{
    public CustomerEntity(string deviceID)
    {
        this.PartitionKey ="flow_meter";
        this.RowKey = deviceID;
    }

    public string username { get; set; }

}



