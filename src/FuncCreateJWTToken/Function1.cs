using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;

namespace FuncCreateJWTToken
{
    //https://social.msdn.microsoft.com/Forums/en-US/0ef65712-0f61-4ffe-bed2-205f97733257/getting-bearer-token-for-calling-azure-logic-apps-rest-apis
    public static class Function1
    {

        [FunctionName("FuncCreateJWTToken")]
        //public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            string requestBody = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            try
            {
                var clientCredential = new ClientCredential((string)data.applicationid, (string)data.clientsecret); //Service Principal Method
                //Sample POST Payload - Service Principal:
                    //{
                    //tenantid": "{AD tentant ID}","applicationid": "{application ID}",	"clientsecret": "{client secret}", "resource": "{azure resource e.g. "https://management.azure.com/"}"
                    //}

               // var clientCredential = new UserPasswordCredential((string)data.username, (string)data.password); //User & Pwd Method
                    //Sample POST Payload - User & Pwd:
                    //{
                    //"tenantid": "{AD tentant ID}", "username": "{username}","password": "{password}", "resource": "{azure resource e.g. "https://management.azure.com/"}"
                    //}
                var authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/{data.tenantid}");
                var result = await authenticationContext.AcquireTokenAsync((string)data.resource, clientCredential);

               // return new OkObjectResult(result.AccessToken);
                
                if (result.AccessToken != null)
                {
                    return new OkObjectResult(result.AccessToken);
                }
                else 
                {
                    return new NotFoundResult();
                }

            }
            catch (System.Exception ex)
            {
                return new NotFoundResult();
                //return req.CreateResponse(HttpStatusCode.BadRequest, $"Error obtaining JWT token. {ex.Message}");
            }
        }
    }
}
