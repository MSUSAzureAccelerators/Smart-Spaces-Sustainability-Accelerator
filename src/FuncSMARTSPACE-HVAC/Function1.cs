using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using Microsoft.Azure.Devices;
using System.Globalization;
using System.Text;
using Message = Microsoft.Azure.Devices.Client.Message;
using System.Text.Json;

namespace FuncHVAC01_HVAC
{

    public static class HVACFunctions
    {
        //private static string deviceName = "smartspace-hvac01-iotdevice";
        private static string deviceName = "smartspace-hvac01-iotdevice";//.ToUpper();
        private static string conn_smart_space_hvac01 = System.Environment.GetEnvironmentVariable("conn_smart_space_hvac01");
        private static string conn_smart_space_hvac01_DeviceTwin = System.Environment.GetEnvironmentVariable("conn_smart_space_hvac01_DeviceTwin");
        private static string conn_smart_space_hvac02 = System.Environment.GetEnvironmentVariable("conn_smart_space_hvac02");
        private static string conn_smart_space_hvac02_DeviceTwin = System.Environment.GetEnvironmentVariable("conn_smart_space_hvac02_DeviceTwin");
        private static string conn_smart_space_hvac03 = System.Environment.GetEnvironmentVariable("conn_smart_space_hvac03");
        private static string conn_smart_space_hvac03_DeviceTwin = System.Environment.GetEnvironmentVariable("conn_smart_space_hvac03_DeviceTwin");

        private static string DEVICE_CLIENT_CONNSTR = "";
        private static string DEVICE_TWIN_CLIENT_CONNSTR = "";

        private static string RUNSTATE = "";
        private static string REALPOWER = "";
        private static float frealpower = 0;

[FunctionName("FuncSMARTSPACE-HVAC")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("FuncSMART-SPACE-HVAC processing a request.");

            string body = await req.ReadAsStringAsync();

            // Parse Incoming JSON Data BODY for the DEVICEID
            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string DEVICEID = root.GetProperty("DeviceID").ToString();
            string switchDevice = DEVICEID.ToUpper();
            switch (switchDevice)
            {
                case "SMARTSPACE-HVAC01-IOTDEVICE":
                    {
                        deviceName = switchDevice.ToLower();
                        DEVICE_CLIENT_CONNSTR = conn_smart_space_hvac01;
                        DEVICE_TWIN_CLIENT_CONNSTR = conn_smart_space_hvac01_DeviceTwin;
                        break;
                    }
                case "SMARTSPACE-HVAC02-IOTDEVICE":
                    {
                        deviceName = switchDevice.ToLower();
                        DEVICE_CLIENT_CONNSTR = conn_smart_space_hvac02;
                        DEVICE_TWIN_CLIENT_CONNSTR = conn_smart_space_hvac02_DeviceTwin;
                        break;
                    }
                case "SMARTSPACE-HVAC03-IOTDEVICE":
                    {
                        deviceName = switchDevice.ToLower();
                        DEVICE_CLIENT_CONNSTR = conn_smart_space_hvac03;
                        DEVICE_TWIN_CLIENT_CONNSTR = conn_smart_space_hvac03_DeviceTwin;
                        break;
                    }
                default:
                    {
                        deviceName = "smartspace-hvac01-iotdevice";
                        DEVICE_CLIENT_CONNSTR = conn_smart_space_hvac01;
                        DEVICE_TWIN_CLIENT_CONNSTR = conn_smart_space_hvac01_DeviceTwin;
                        break;
                    }
            }

            log.LogInformation("FuncSMART-SPACE-HVAC -> DEVICEID = " + DEVICEID);
            log.LogInformation("FuncSMART-SPACE-HVAC -> DEVICE_CLIENT_CONNSTR = " + DEVICE_CLIENT_CONNSTR);
            log.LogInformation("FuncSMART-SPACE-HVAC -> DEVICE_TWIN_CLIENT_CONNSTR = " + DEVICE_TWIN_CLIENT_CONNSTR);

            using var deviceClient = DeviceClient.CreateFromConnectionString(DEVICE_CLIENT_CONNSTR);

            //// Set various callback messages: connection status, device twin, a single named method, multiple methods
            //SetVariousCallbackMethods(deviceClient);

            // open connection explicitly
            deviceClient.OpenAsync().Wait();
            //----------------------------------------------------------------
            //Retreive Device Twin Properties
            //----------------------------------------------------------------
            string desiredprop = "CURRTEMP";
            string CURRTEMP = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("CURRTEMP: " + CURRTEMP);

            desiredprop = "CHILL_RATE";
            string CHILLRATE = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("CHILL_RATE: " + CHILLRATE);

            desiredprop = "REALPOWER";
            REALPOWER = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("REALPOWER: " + REALPOWER);

            desiredprop = "REALPOWER_RATE";
            string REALPOWERRATE = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("REALPOWER_RATE: " + REALPOWERRATE);

            desiredprop = "RUN_STATE";
            //string RUNSTATE = DeviceTwinGET_Props(deviceClient, desiredprop);
            RUNSTATE = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("RUN_STATE: " + RUNSTATE);

            desiredprop = "H2O_SETPOINT";
            string H2OSETPOINT = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("H2O_SETPOINT: " + H2OSETPOINT);

            desiredprop = "H2O_TEMP_ENTER";
            string H2OTEMPENTER = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("H2O_TEMP_ENTER: " + H2OTEMPENTER);

            desiredprop = "H2O_TEMP_LEAVE";
            string H2OTEMPLEAVE = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("H2O_TEMP_LEAVE: " + H2OTEMPLEAVE);

            desiredprop = "LASTUPDT";
            string LASTUPDT = DeviceTwinGET_Props(deviceClient, desiredprop);
            log.LogInformation("LASTUPDT: " + desiredprop);

            //Convert to FLOATS
            float fcurrtemp = float.Parse(CURRTEMP, CultureInfo.InvariantCulture.NumberFormat);
            float ftemprate = float.Parse(CHILLRATE, CultureInfo.InvariantCulture.NumberFormat);
            float fsetpoint = float.Parse(H2OSETPOINT, CultureInfo.InvariantCulture.NumberFormat);

            frealpower = float.Parse(REALPOWER, CultureInfo.InvariantCulture.NumberFormat);
            float frealpowerrate = float.Parse(REALPOWERRATE, CultureInfo.InvariantCulture.NumberFormat);
            float ftempenter = float.Parse(H2OTEMPENTER, CultureInfo.InvariantCulture.NumberFormat);
            float ftempleave = float.Parse(H2OTEMPLEAVE, CultureInfo.InvariantCulture.NumberFormat);

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
            SendDeviceToCloudMessagesAsync(deviceClient, fwrktemp, fsetpoint, frealpower, ftempenter, ftempleave, RUNSTATE,log);
            log.LogInformation(deviceName + " - Sending Message... ");// + fwrktemp.ToString() + " " + fsetpoint.ToString());

            //SAVE Settings - CURRTEMP
            string patch = "{ \"properties\": { \"desired\": { \"CURRTEMP\" : " + "'" + fwrktemp.ToString() + "'" + " } } }"; //json string
            UpdateDeviceTwinPropPATCH(patch);
            log.LogInformation(deviceName + " - SAVE Settings - CURRTEMP ");
            Thread.Sleep(1000);//Sleep for 1 Seconds

            //SAVE Settings - LASTUPDT
            string CurrDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.f");
            //log.LogInformation("Current DateTime is '{0}' to a date and time.", CurrDateTime);
            patch = "{ \"properties\": { \"desired\": { \"LASTUPDT\" : " + "'" + CurrDateTime.ToString() + "'" + " } } }"; //json string
            log.LogInformation(deviceName + " - SAVE Settings - LASTUPDT ");
            UpdateDeviceTwinPropPATCH(patch);
            Thread.Sleep(1000);//Sleep for 1 Seconds

            ///Receive Commands
            await ReceiveCommands(deviceClient);
            Thread.Sleep(1000);//Sleep for 1 Seconds


            string responseMessage = "Function " + deviceName + " executed successfully.";
            return new OkObjectResult(responseMessage);
        }
        private static async void SendDeviceToCloudMessagesAsync(DeviceClient deviceClient, float fwrktemp, float fsetpoint, float frealpower, float ftempenter, float ftempleave, string RUNSTATE,ILogger log)
        {
            var telemetryDataPoint = new
            {
                deviceId = deviceName,
                currentTemperature = fwrktemp,
                setpoint = fsetpoint,
                realpower = frealpower,
                tempenter = ftempenter,
                templeave = ftempleave,
                runstate = RUNSTATE
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            //var message = new Message(Encoding.ASCII.GetBytes(messageString));
            Microsoft.Azure.Devices.Client.Message message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(messageString));
            await deviceClient.SendEventAsync(message);
            log.LogInformation("{0} > Sending message: {1}", DateTime.Now, messageString);
            Thread.Sleep(1500);//Sleep for 1/2 Second
        }
        private static string DeviceTwinGET_Props(DeviceClient deviceClient, string desiredprop)
        {
            var twin = deviceClient.GetTwinAsync().Result;
            string rtnprop = twin.Properties.Desired[desiredprop];
            return rtnprop;
        }
        private static async void UpdateDeviceTwinPropPATCH(string patch)
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(DEVICE_TWIN_CLIENT_CONNSTR);
            var twin = await registryManager.GetTwinAsync(deviceName);

            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
            
            //log.LogInformation("The Devicetwin has been UPDATED!");
        }
        private static void SetVariousCallbackMethods(DeviceClient deviceClient)
        {
            deviceClient.SetMethodHandlerAsync("Ping", HandleMethodPing, deviceClient).ConfigureAwait(false);

            var thread = new Thread(() => ThreadBody(deviceClient));
            thread.Start();
            Console.WriteLine("Callback methods are set");
        }
        private static Task<MethodResponse> HandleMethodPing(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"method {methodRequest.Name} handled with body {methodRequest.DataAsJson}");
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }
        private static async void ThreadBody(object deviceClient)
        {
            var client = deviceClient as DeviceClient;
            Console.WriteLine("Waiting for C2D messages (aka commands)");

            // The following line is blocking until a timeout occurs
            TimeSpan timeout = new TimeSpan(0, 0, 0, 0, 250);
            using var message = await client.ReceiveAsync();
            if (message == null)
            {
                Console.WriteLine("Timeout. Command is null");
            }
            string data = Encoding.UTF8.GetString(message.GetBytes());
            Console.WriteLine($"A message is received with body {data}");
            await client.CompleteAsync(message); // mark the message as handled
                                                 //await client.RejectAsync(message); // drops the message as unhandled
                                                 //await client.AbandonAsync(message); // puts message back on queue
        }
        static async Task ReceiveCommands(DeviceClient deviceClient)
        {
            Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Message receivedMessage;
            string messageData;
            //while (true)
            //{
            receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(1));

            if (receivedMessage != null)
            {
                messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                await deviceClient.CompleteAsync(receivedMessage);

                string txt = messageData.ToUpper();
                //txt = String.ToUpper(txt);
                
                string caseSwitch = txt; // = String.ToUpper(txt);
                switch (caseSwitch)
                {
                    case "PING":
                        Console.WriteLine("I was PINGED!!!");
                        break;
                    case "START":
                        Console.WriteLine("START");
                        RUNSTATE = "Running";
                        string patch = "{ \"properties\": { \"desired\": { \"RUN-STATE\" : " + "'" + "Running" + "'" + " } } }"; //json string
                        UpdateDeviceTwinPropPATCH(patch);
                        Thread.Sleep(1000);//Sleep for 1 Seconds
                        break;
                    case "STOP":
                        Console.WriteLine("STOP");
                        RUNSTATE = "Stopped";
                        patch = "{ \"properties\": { \"desired\": { \"RUN-STATE\" : " + "'" + "Stopped" + "'" + " } } }"; //json string
                        UpdateDeviceTwinPropPATCH(patch);
                        Thread.Sleep(1000);//Sleep for 1 Seconds

                        patch = "{ \"properties\": { \"desired\": { \"REALPOWER\" : " + "'" + "0" + "'" + " } } }"; //json string
                        UpdateDeviceTwinPropPATCH(patch);
                        Thread.Sleep(1000);//Sleep for 1 Seconds
                        frealpower = 0;

                        break;
                }
                
            }
        }
    }
}
