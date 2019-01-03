/* 	Usage: Lambda function to add FlowEntity (See class at bottom) to Azure Table Storage
 * 
 *	Configuration notes:
 *	Trigger: HTTP (req)
 *	Inputs: Azure Table Storage (inTable)
 *	Outputs: HTTP ($return)
 *
 *	Responds to HTTP GET and POST with whether or not there has been flow recieved in the last 1 minute (Indicates the device is in use).
 * 	
 *	Request Parameter Name: req
 * 	Authorization Level: Function
 *
 */
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage.Table;
 
/* Called to determine if water detected on flow meter */
// NOTE: Static device so far
public static async Task<HttpResponseMessage> Run(HttpRequest req, ILogger log, CloudTable inputTable)
{
    log.LogInformation("C# HTTP trigger function processed a request.");

    string textToSpeech;

    if (await DetermineIfRunning(inputTable, log)) {
        textToSpeech = "The shower is currently occupied.";
    } else {
        textToSpeech = "The shower is currently unoccupied";
    }

    return await CreateResponse(textToSpeech);    
}

private static async Task<HttpResponseMessage> CreateResponse(string textToSpeech) {
    // Construct response
    dynamic responses = new JObject();
    responses.textToSpeech = textToSpeech;

    dynamic simpleResponses = new JObject();
    simpleResponses.simpleResponses = new JObject();
    simpleResponses.simpleResponses.simpleResponses = new JArray(responses);

    dynamic product = new JObject();
    product.fulfillmentText = "fulfillmentText";
    product.fulfillmentMessages = new JArray(simpleResponses);
    product.source = "webhook-sample";
     
    return new HttpResponseMessage(HttpStatusCode.OK) 
    {
        Content = new StringContent(product.ToString(), Encoding.UTF8, "application/json")
    };
}

/* Not sustainable of getting all in partition */
private static async Task<bool> DetermineIfRunning(CloudTable inputTable, ILogger log) {
    // Construct the query operation for all customer entities where PartitionKey="Smith".
    TableQuery<FlowEntity> query = new TableQuery<FlowEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "00000001"));

    // Print the fields for each customer.
    TableContinuationToken token = null;

    // Get time from 1 minute earlier
    DateTime utcDate = DateTime.UtcNow;
    utcDate = utcDate.Add(new TimeSpan(0,-1,0));

    do
    {
       TableQuerySegment<FlowEntity> resultSegment = await inputTable.ExecuteQuerySegmentedAsync(query, token);
       token = resultSegment.ContinuationToken;

       foreach (FlowEntity entity in resultSegment.Results)
       {
           log.LogInformation("utcDate: " + utcDate);
           log.LogInformation("storedDate: " + DateTime.ParseExact(entity.RowKey, "yyyyMMddHHmmssffff", null));
           // Check if row is within 1 min of now
            if (DateTime.Compare(utcDate, DateTime.ParseExact(entity.RowKey, "yyyyMMddHHmmssffff", null)) < 0) {
                return true;
            }
      }
    } while (token != null);

    return false;
}

public class FlowEntity : TableEntity
{
    public FlowEntity(string deviceID)
    {
        this.PartitionKey = deviceID;
        this.RowKey = (DateTime.UtcNow).ToString("yyyyMMddHHmmssffff");
    }

    public FlowEntity() { }

    public string flow { get; set; }
}



