using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using System.Text;
using Microsoft.Azure.Devices;

namespace FuncSMARTSPACE
{
    public static class Function1
    {
        private static string deviceName = "SMARTSPACE-IOTDEVICE";
        private static string conn_iotHubConnectionStringDevice = System.Environment.GetEnvironmentVariable("iotHubConnectionStringDevice");
        private static string conn_iotHubConnectionStringDeviceTwin = System.Environment.GetEnvironmentVariable("iotHubConnectionStringDeviceTwin");
        private static string conn_smart_space_device = System.Environment.GetEnvironmentVariable("conn_smart_Space");
        private static string conn_smartspace_twin = System.Environment.GetEnvironmentVariable("conn_smart_Space_DeviceTwin");

        [FunctionName("FuncSMARTSPACE")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("FuncSMARTSPACE processing a request.");
            log.LogInformation("FuncSMARTSPACE - IOTHubConnStr = " + conn_smart_space_device);
            log.LogInformation("FuncSMARTSPACE - iotHubConnectionStringDeviceTwin = " + conn_smartspace_twin);

            log.LogInformation("FuncSMARTSPACE - conn_smart_space_connstr = " + conn_smart_space_device);  //conn_smart_space_device);
            log.LogInformation("FuncSMARTSPACE - conn_smartspace_twin = " + conn_smartspace_twin);  //conn_smartspace_twin);

            //using var deviceClient = DeviceClient.CreateFromConnectionString(conn_smart_space_device);
            using var deviceClient = DeviceClient.CreateFromConnectionString(conn_smart_space_device);
            log.LogInformation("DeviceClient Created successfuly... ");

            // open connection explicitly
            deviceClient.OpenAsync().Wait();
            log.LogInformation("DeviceClient Opened successfuly... ");

            //----------------------------------------------------------------
            //Retreive Device Twin Properties
            //----------------------------------------------------------------
            string desiredprop = "CURRTEMP";
            string CURRTEMP = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("CURRTEMP: " + CURRTEMP);
            
            desiredprop = "SETPOINT";
            string SETPOINT = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("SETPOINT: " + SETPOINT);
            
            desiredprop = "CHILL_RATE";
            string CHILLRATE = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("CHILLRATE: " + CHILLRATE);

            desiredprop = "LASTUPDT";
            string LASTUPDT = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("LASTUPDT: " + LASTUPDT);

            float fcurrtemp = float.Parse(CURRTEMP, CultureInfo.InvariantCulture.NumberFormat);
            float ftemprate = float.Parse(CHILLRATE, CultureInfo.InvariantCulture.NumberFormat);
            float fsetpoint = float.Parse(SETPOINT, CultureInfo.InvariantCulture.NumberFormat);
            //--------------------------------------------------------------------------
            // Calculate NEW CURR TEMP - based on Hourly rate -> Minute Rate
            //--------------------------------------------------------------------------            

            //CONVERT LAST DATETIME RETRIEVED from DeviceTwiin
            string datePattern = "yyyy-MM-dd HH:mm:ss.f";
            DateTime parsedDateTime;
            if (DateTime.TryParseExact(LASTUPDT, datePattern, null, DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                log.LogInformation("Converted '{0}' to {1:d}.", LASTUPDT, parsedDateTime);
            else
                log.LogInformation("Unable to convert '{0}' to a date and time.", LASTUPDT);
            LASTUPDT = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f");

            //Calulate Nbr of minutes since last update
            DateTime dt2 = DateTime.Now;
            double totalminutes = (dt2 - parsedDateTime).TotalMinutes;
            log.LogInformation("No. of Minutes (Difference) = {0}", totalminutes);

            // Convert Rate from Hourly to per Minute rate
            double ftempratemin = ftemprate / 60;
            log.LogInformation("Minute Rate  is '{0}' ", ftempratemin.ToString());

            //Extend total minutes by rate per Minute
            double fwrktemprate = totalminutes * ftempratemin;
            log.LogInformation("Calc Temp is  '{0}' ", fwrktemprate.ToString());
            
            //Calc New Temperature
            double dwrktemp = fcurrtemp - Math.Abs(fwrktemprate);
            log.LogInformation("Calc NEW Temp is  '{0}' ", dwrktemp.ToString());

            float fwrktemp = (float)dwrktemp;

            //Check if we reached the SETPOINT - If So, STAY At SETPOINT
            if (fwrktemp < fsetpoint)
            {
                fwrktemp = fsetpoint;
            }

            //Send D2C Message
            SendDeviceToCloudMessagesAsync(deviceClient, fwrktemp, fsetpoint);
            log.LogInformation("FuncSMARTSPACE - Sending Message " + fwrktemp.ToString() + " " + fsetpoint.ToString());

            //SAVE Settings - CURRTEMP
            string patch = "{ \"properties\": { \"desired\": { \"CURRTEMP\" : " + "'" + fwrktemp.ToString() + "'" + " } } }"; //json string
            UpdateDeviceTwinPropPATCH(patch);

            Thread.Sleep(1000);//Sleep for 1 Seconds

            //SAVE Settings - LASTUPDT
            string CurrDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f");
            //log.LogInformation("Current DateTime is '{0}' to a date and time.", CurrDateTime);
            patch = "{ \"properties\": { \"desired\": { \"LASTUPDT\" : " + "'" + CurrDateTime.ToString() + "'" + " } } }"; //json string
            UpdateDeviceTwinPropPATCH(patch);
            Thread.Sleep(1000);//Sleep for 1 Seconds
            string responseMessage = "Function SMARTSPACE executed successfully.";
            return new OkObjectResult(responseMessage);
        }
        private static async void SendDeviceToCloudMessagesAsync(DeviceClient deviceClient, float fwrktemp, float fsetpoint)
        {
            var telemetryDataPoint = new
            {
                deviceId = deviceName,
                currentTemperature = fwrktemp,
                setpoint = fsetpoint
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            //var message = new Message(Encoding.ASCII.GetBytes(messageString));
            Microsoft.Azure.Devices.Client.Message message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(messageString));
            await deviceClient.SendEventAsync(message);
            Console.Write("{0} > Sending message: {1}", DateTime.Now, messageString);
            Thread.Sleep(500);//Sleep for 1/2 Second
        }
        private static string DeviceTwinGET_Props(DeviceClient deviceClient, string desiredprop)
        {
            var twin = deviceClient.GetTwinAsync().Result;
            string rtnprop = twin.Properties.Desired[desiredprop];
            return rtnprop;
        }
        private static async void UpdateDeviceTwinPropPATCH(string patch)
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(conn_smartspace_twin);
            var twin = await registryManager.GetTwinAsync("smartspace-iotdevice");
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
            Console.Write("The Devicetwin has been UPDATED!");
        }
    }
    internal class TempData
    {
        public string Id { get; internal set; }
        public DateTime Dttm { get; internal set; }
        public string Name { get; internal set; }
        public string CurrTemp { get; internal set; }
    }
}
