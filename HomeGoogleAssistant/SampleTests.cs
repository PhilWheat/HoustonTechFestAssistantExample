using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Google.Protobuf;
using Google.Cloud.Dialogflow.V2;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace HomeGoogleAssistant
{
	public class GoogleAssistantCall : TableEntity
	{
		public string CalledParameters { get; set; }

		public GoogleAssistantCall(string callParamters)
		{
			this.PartitionKey = "GoogleAssistant";
			this.RowKey = DateTime.Now.Ticks.ToString();
			this.CalledParameters = callParamters;
		}
	}

	public static class SampleTests
    {
        private static readonly JsonParser jsonParser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

        [FunctionName("SampleTests")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
			WebhookRequest request;
			var response = new WebhookResponse();

			string body = await new StreamReader(req.Body).ReadToEndAsync();
			request = jsonParser.Parse<WebhookRequest>(body);


			// Table Store setup
			CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials("{CredentialsHere}", "{CredentialsHere}"), true);
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
			CloudTable requestsTable = tableClient.GetTableReference("GoogleAssistantLog");

			GoogleAssistantCall callInfo = new GoogleAssistantCall(body);
			TableOperation insertRow = TableOperation.InsertOrMerge(callInfo);
			requestsTable.ExecuteAsync(insertRow);

			var pas = request.QueryResult.Parameters;
			var askingName = pas.Fields.ContainsKey("Name") && pas.Fields["Name"].ToString().Replace('\"', ' ').Trim().Length > 0;
			var askingAddress = pas.Fields.ContainsKey("Address") && pas.Fields["Address"].ToString().Replace('\"', ' ').Trim().Length > 0;
			var askingBusinessHour = pas.Fields.ContainsKey("Business-hours") && pas.Fields["Business-hours"].ToString().Replace('\"', ' ').Trim().Length > 0;

			string name = "Jeffson Library", address = "1234 Brentwood Lane, Dallas, TX 12345", businessHour = "8:00 am to 8:00 pm";

			StringBuilder sb = new StringBuilder();

			if (askingName)
			{
				sb.Append("The name of the library is: " + name + "; ");
			}

			if (askingAddress)
			{
				sb.Append("The Address of the library is: " + address + "; ");
			}

			if (askingBusinessHour)
			{
				sb.Append("The Business Hours for the library are: " + businessHour + "; ");
			}

			if ((request.QueryResult.QueryText == "what time is it?") |
				(request.QueryResult.QueryText == "what time is it"))
			{
				sb.Append("The time is currently " + DateTime.Now.TimeOfDay.ToString());
			}

			if (request.QueryResult.QueryText == "what is the date")
			{
				sb.Append("The date is currently " + DateTime.Now.Date.ToString());
			}

			if (sb.Length == 0)
			{
				sb.Append("Greetings from our Webhook API!");
			}

			response.FulfillmentText = sb.ToString();

			return (ActionResult)new OkObjectResult(response);
		}
    }
}
