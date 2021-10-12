using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace LGSTrayBattery_GHUB
{
    class HttpServer
    {
        public static bool ServerEnabled;
        private static string _tcpAddr;
        private static int _tcpPort;
        private static bool _loaded = false;

        public static void LoadConfig()
        {
            var parser = new FileIniDataParser();

            if (!File.Exists("./HttpConfig.ini"))
            {
                File.Create("./HttpConfig.ini").Close();
            }

            IniData data = parser.ReadFile("./HttpConfig.ini");

            if (!bool.TryParse(data["HTTPServer"]["serverEnable"], out ServerEnabled))
            {
                data["HTTPServer"]["serverEnable"] = "false";
            }

            if (!int.TryParse(data["HTTPServer"]["tcpPort"], out _tcpPort))
            {
                data["HTTPServer"]["tcpPort"] = "12321";
            }

            _tcpAddr = data["HTTPServer"]["tcpAddr"];
            if (_tcpAddr == null)
            {
                data["HTTPServer"]["tcpAddr"] = "localhost";
            }

            parser.WriteFile("./HttpConfig.ini", data);
        }

        public static void ServerLoop(MainWindowViewModel viewModel)
        {
            if (!_loaded)
            {
                LoadConfig();
            }

            if (!ServerEnabled)
            {
                return;
            }

            Debug.WriteLine("\nHttp Server starting");

            IPAddress ipAddress;
            if (!IPAddress.TryParse(_tcpAddr, out ipAddress))
            {
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(_tcpAddr);
                    ipAddress = host.AddressList[0];
            }
                catch (SocketException)
            {
                    Debug.WriteLine("Invalid hostname, defaulting to loopback");
                    ipAddress = IPAddress.Loopback;
            }

            }

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _tcpPort);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);

            Debug.WriteLine($"Http Server listening on {localEndPoint}\n");

            while (true)
            {
                using (Socket client = listener.Accept())
                {
                    try
                    {
                        var bytes = new byte[1024];
                        var bytesRec = client.Receive(bytes);

                        string httpRequest = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                        var matches = Regex.Match(httpRequest, @"GET (.+?) HTTP\/[0-9\.]+");
                        if (matches.Groups.Count > 0)
                        {
                            int statusCode = 400;
                            string contentType = "text";
                            string content = "";

                            string[] request = matches.Groups[1].ToString()
                                .Split(new string[] {"/"}, StringSplitOptions.RemoveEmptyEntries);
                            string rootRequest = request.Length > 0 ? request[0] : "";

                            if (rootRequest == "devices")
                            {
                                statusCode = 200;
                                contentType = "text/html";
                                content = "<html>";

                                foreach (var device in viewModel.DeviceList)
                                {
                                    content +=
                                        $"{device.DisplayName} : <a href=\"/device/{device.DeviceId}\">{device.DeviceId}</a><br>";
                                }

                                content += "</html>";
                            }
                            else if (rootRequest == "device")
                            {
                                string deviceId = request.Length > 1 ? request[1] : "";
                                Device targetDevice = viewModel.DeviceList.FirstOrDefault(x => x.DeviceId == deviceId);

                                if (targetDevice == null)
                                {
                                    statusCode = 404;
                                    content = $"Device not found, ID = {request[1]}";
                                }
                                else
                                {
                                    statusCode = 200;
                                    content = targetDevice.ToXml();
                                    contentType = "text/xml";
                                }
                            }

                            string response = $"HTTP/1.1 {statusCode}\r\n" +
                                              $"Content-Type: {contentType}\r\n" +
                                              $"Access-Control-Allow-Origin: *\r\n" +
                                              $"Cache-Control: no-store, must-revalidate\r\n" +
                                              $"Pragma: no-cache\r\n" +
                                              $"Expires: 0\r\n" +
                                              $"\r\n{content}";

                            client.Send(Encoding.ASCII.GetBytes(response));
                        }
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        throw ex;
                        #endif

                        StringBuilder sb = new StringBuilder();

                        while (ex != null)
                        {
                            sb.AppendLine("-----------------------------------------------------------------------------");
                            sb.AppendLine(ex.GetType().FullName);
                            sb.AppendLine("Message :");
                            sb.AppendLine(ex.Message);
                            sb.AppendLine("StackTrace :");
                            sb.AppendLine(ex.StackTrace);
                            sb.AppendLine();

                            ex = ex.InnerException;
                        }

                        string response = $"HTTP/1.1 400\r\n" +
                                          $"Content-Type: text\r\n" +
                                          $"Access-Control-Allow-Origin: *\r\n" +
                                          $"Cache-Control: no-store, must-revalidate\r\n" +
                                          $"Pragma: no-cache\r\n" +
                                          $"Expires: 0\r\n" +
                                          $"\r\n{sb}";

                        client.Send(Encoding.ASCII.GetBytes(response));
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}
