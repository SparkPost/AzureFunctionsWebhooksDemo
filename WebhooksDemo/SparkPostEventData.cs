using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;

namespace WebhooksDemo
{
    public static class SparkPostEventData
    {
        [FunctionName("SparkPostEventData")]
        public static async Task<object> Run([HttpTrigger("POST", WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"Webhook was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            if (data.message_event == null || data.message_event.GetType() != typeof(Newtonsoft.Json.Linq.JObject))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Data does not contain properly formatted message event."
                });
            }
            if (data.message_event.events == null || data.message_event.events.GetType() != typeof(Newtonsoft.Json.Linq.JObject))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Data does not contain properly formatted bounce event."
                });
            }
            if (data["message_event"]["events"]["bounce"]["event"]["type"] == "bounce")
            {
                if (data["message_event"]["events"]["bounce"]["event"]["error_code"].ToString().StartsWith("5"))
                {
                    if (data["message_event"]["events"]["bounce"]["event"]["rcpt_to"] != null)
                    {
                        string recipient = data["message_event"]["events"]["bounce"]["event"]["rcpt_to"];
                        /* here we would process the hard bounce by removing the email address from our lists
                           if this process takes more than a second or two, it should be run async
                           or even passed off to another service */
                        return req.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = $"Hard bounce processed for {recipient}"
                        });
                    }
                    else
                    {
                        return req.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            error = "Bounce event does not contain recipient data."
                        });
                    }
                }
                else
                {
                    // not a hard bounce (5xx) so just acknowledge for now
                    return req.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = "Soft bounce data accepted."
                    });
                }
            }
            return req.CreateResponse(HttpStatusCode.BadRequest, new
            {
                error = "Unprocessable data."
            });
        }
    }
}
