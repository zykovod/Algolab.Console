using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AlgolabAPI
{
    public class Program
    {
        public static string USERNAME = "";
        public static string PASSWORD = "";
        public static string APIKEY = "";

        public static string hostname = "https://www.algolab.com.tr";
        public static string apiurl = "https://www.algolab.com.tr/api";
        public static string websocketurl = "wss://www.algolab.com.tr/api/ws";

        public static string URL_LOGIN_USER = "/api/LoginUser";
        public static string URL_LOGIN_CONTROL = "/api/LoginUserControl";
        public static string URL_SESSIONREFRESH = "/api/SessionRefresh";
        public static string URL_GETEQUITYINFO = "/api/GetEquityInfo";
        public static string URL_GETSUBACCOUNTS = "/api/GetSubAccounts";
        public static string URL_INSTANTPOSITION = "/api/InstantPosition";
        public static string URL_VIOPCOLLATERALINFO = "/api/ViopCollateralInfo";
        //public static string URL_RISKSIMULATION = "/api/RiskSimulation";
        public static string URL_TODAYTRANSACTION = "/api/TodaysTransaction";
        public static string URL_VIOPCUSTOMEROVERALL = "/api/ViopCustomerOverall";
        public static string URL_VIOPCUSTOMERTRANSACTIONS = "/api/ViopCustomerTransactions";
        public static string URL_SENDORDER = "/api/SendOrder";
        public static string URL_MODIFYORDER = "/api/ModifyOrder";
        public static string URL_DELETEORDER = "/api/DeleteOrder";
        public static string URL_DELETEORDERVIOP = "/api/DeleteOrderViop";
        public static string URL_GETCANDLEDATA = "/api/GetCandleData";
        //public static string URL_GETEQUITYORDERHISTORY = "/api/GetEquityOrderHistory";
        public static string URL_GETVIOPORDERHISTORY = "/api/GetViopOrderHistory";
        //public static string URL_ACCOUNTEXTRE = "/api/AccountExtre";
        //public static string URL_CASHFLOW = "/api/CashFlow";

        public static string HASH = "";
        public static string SMSCODE = "";

        private static WebSocketApi wsApi;

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello!\nProcess UserLogin...");
            Response login;
            string token;
            try
            {
                login = LoginUser(USERNAME, PASSWORD);
                Console.WriteLine(SerializeJson(login));
                token = login.Content.token;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return;
            }

            Console.Write("Sms code: ");
            SMSCODE = Console.ReadLine();

            Console.WriteLine("Process UserLoginControl...");
            dynamic control;
            try
            {
                control = LoginControl(token, SMSCODE);
                Console.WriteLine(SerializeJson(control));
                HASH = control.Content.hash;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return;
            }

            string symbol = "F_XAUTRYM0424";

            Console.WriteLine("Process Websocket...");
            wsApi = new WebSocketApi(APIKEY, HASH, ComputeSha256Hash(APIKEY + hostname + "/ws"));
            wsApi.Connect(websocketurl).Wait();
            if (wsApi.IsOpen)
            {
                Console.WriteLine("Websocket opened");
                wsApi.SubscribeDepth(symbol);
                wsApi.SubscribeTrade(symbol);
            }
            else
                Console.WriteLine("Websocket unable to open");

            Console.WriteLine("Input the command ('list' displays all of them):");

            while (true)
            {
                Console.Write("> ");
                var cmd = Console.ReadLine().ToUpper();

                switch (cmd)
                {
                    case "LIST":
                        Console.WriteLine(
                            "UPD - SessionRefresh\n" +
                            "LIM - ViopCollateralInfo\n" +
                            "POS - ViopCustomerOverall\n" +
                            "SEND - send order\n" +
                            "MOD - modify viop order\n" +
                            "DEL - cancel viop order\n" +
                            "DAILY - get today transactions\n" +
                            "TRANS - get viop transactions\n" +
                            "HIST - get viop order history\n" +
                            "END - close the console"
                        );
                        break;

                    case "UPD":
                        Console.WriteLine("Result of SessionRefresh: " + SessionRefresh());
                        break;

                    case "LIM":
                        Console.WriteLine("Result of ViopCollateralInfo: " + SerializeJson(ViopCollateralInfo("")));
                        break;

                    case "POS":
                        Console.WriteLine("Result of ViopCustomerOverall: " + SerializeJson(ViopCustomerOverall("")));
                        break;

                    case "SEND":
                        Console.WriteLine("Input parameters of order: <BUY|SELL> <market|limit> <price> <lot>");
                        Console.Write(">> ");
                        var send = Console.ReadLine();
                        var sendArr = send.Split(' ');
                        if (sendArr.Length == 4)
                        {
                            Console.WriteLine(SerializeJson(SendOrder(symbol, sendArr[0], sendArr[1], sendArr[2], sendArr[3], false, false, "")));
                        }
                        else
                            Console.WriteLine("Incorrect input string");
                        break;

                    case "MOD":
                        Console.WriteLine("Input parameters for modify order: <id> <price> <lot>");
                        Console.Write(">> ");
                        var modify = Console.ReadLine();
                        var modifyArr = modify.Split(' ');
                        if (modifyArr.Length == 3)
                        {
                            Console.WriteLine(SerializeJson(ModifyOrder(modifyArr[0], modifyArr[1], modifyArr[2], true, "")));
                        }
                        else
                            Console.WriteLine("Incorrect input string");
                        break;

                    case "DEL":
                        Console.WriteLine("Input parameters to delete order: <id> <qty>");
                        Console.Write(">> ");
                        var cancel = Console.ReadLine();
                        var cancelArr = cancel.Split(' ');
                        if (cancelArr.Length == 2)
                        {
                            Console.WriteLine(SerializeJson(DeleteOrderViop(cancelArr[0], cancelArr[1], "")));
                        }
                        else
                            Console.WriteLine("Incorrect input string");
                        break;

                    case "DAILY":
                        Console.WriteLine("Result of TodaysTransaction: " + SerializeJson(TodaysTransaction("")));
                        break;

                    case "TRANS":
                        Console.WriteLine("Result of ViopCustomerTransactions: " + SerializeJson(ViopCustomerTransactions("")));
                        break;

                    case "HIST":
                        Console.WriteLine("Input id for history order:");
                        Console.Write(">> ");
                        var history = Console.ReadLine();
                        Console.WriteLine(SerializeJson(GetViopOrderHistory(history, "")));
                        break;

                    case "END":
                        if (wsApi.IsOpen)
                            wsApi.Stop().Wait();
                        return;
                }

                //Thread.Sleep(100);
            }

            //var Equity = GetEquityInfo(symbol);
            //var SubAccounts = GetSubAccounts();
            //var instantPosition = InstantPosition("");
            //var todaysTransaction = TodaysTransaction("");
            //var viopCustomerOverall = ViopCustomerOverall("");
            //var viopCustomerTransactions = ViopCustomerTransactions("");
            //var sendOrder = SendOrder("TSKB", "BUY", "piyasa", "", "1", true, true, "");
            //var sendOrder2 = SendOrder("TSKB", "BUY", "piyasa", "", "1", true, true, "");
            //string orderid = sendOrder.Content.ToString().Split(';')[0].Split(":")[1].Trim();
            //var modifyOrder = ModifyOrder(orderid, "3.91", "1", false, "");
            //var deleteOrder = DeleteOrder(orderid, "");
            //var sessionRefresh = SessionRefresh();
            //var getcandledata = DeserializeJson<List<Bar>>(SerializeJson(GetCandleData("GARAN", "1").Content));
            //var getcandledata2 = DeserializeJson<List<Bar>>(SerializeJson(GetCandleData("CCOLA", "1").Content));
        }

        public static Response LoginUser(string username, string password)
        {
            try
            {
                string eUsername = OpenSSLEncryptApi(username, APIKEY.Split('-')[1]);
                string ePassword = OpenSSLEncryptApi(password, APIKEY.Split('-')[1]);

                string postData = "{\"Username\":\"" + eUsername + "\",\"Password\":\"" + ePassword + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_LOGIN_USER);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);

                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response LoginControl(string token, string smscode)
        {
            try
            {
                string eToken = OpenSSLEncryptApi(token, APIKEY.Split('-')[1]);
                string eSmscode = OpenSSLEncryptApi(smscode, APIKEY.Split('-')[1]);

                string postData = "{\"token\":\"" + eToken + "\",\"Password\":\"" + eSmscode + "\"}";


                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_LOGIN_CONTROL);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);

                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response GetEquityInfo(string symbol)
        {
            try
            {
                string postData = "{\"symbol\":\"" + symbol + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_GETEQUITYINFO);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_GETEQUITYINFO + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response GetSubAccounts()
        {
            try
            {
                string postData = "";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_GETSUBACCOUNTS);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_GETSUBACCOUNTS + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response InstantPosition(string Subaccount)
        {
            try
            {
                string postData = "{\"subaccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_INSTANTPOSITION);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_INSTANTPOSITION + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response TodaysTransaction(string Subaccount)
        {
            try
            {
                string postData = "{\"Subaccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_TODAYTRANSACTION);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_TODAYTRANSACTION + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response ViopCollateralInfo(string Subaccount)
        {
            try
            {
                string postData = "{\"Subaccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_VIOPCOLLATERALINFO);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_VIOPCOLLATERALINFO + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response ViopCustomerOverall(string Subaccount)
        {
            try
            {
                string postData = "{\"Subaccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_VIOPCUSTOMEROVERALL);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_VIOPCUSTOMEROVERALL + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response ViopCustomerTransactions(string Subaccount)
        {
            try
            {
                string postData = "{\"Subaccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_VIOPCUSTOMERTRANSACTIONS);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_VIOPCUSTOMERTRANSACTIONS + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response SendOrder(string symbol, string direction, string pricetype, string price, string lot, bool sms, bool email, string Subaccount)
        {
            try
            {
                string postData = "{\"symbol\":\"" + symbol + "\",\"direction\":\"" + direction + "\",\"pricetype\":\"" + pricetype + "\",\"price\":\"" + price + "\",\"lot\":\"" + lot + "\",\"sms\":" + sms.ToString().ToLower() + ",\"email\":" + email.ToString().ToLower() + ",\"subAccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_SENDORDER);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_SENDORDER + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response ModifyOrder(string id, string price, string lot, bool viop, string Subaccount)
        {
            try
            {
                string postData = "{\"id\":\"" + id + "\",\"price\":\"" + price + "\",\"lot\":\"" + lot + "\",\"viop\":" + viop.ToString().ToLower() + ",\"subAccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_MODIFYORDER);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_MODIFYORDER + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response DeleteOrder(string id, string Subaccount)
        {
            try
            {
                string postData = "{\"id\":\"" + id + "\",\"subAccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_DELETEORDER);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_DELETEORDER + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response DeleteOrderViop(string id, string adet, string Subaccount)
        {
            try
            {
                string postData = "{\"id\":\"" + id + "\",\"adet\":\"" + adet + "\",\"subAccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_DELETEORDERVIOP);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_DELETEORDERVIOP + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response GetCandleData(string symbol, string period)
        {
            try
            {
                string postData = "{\"symbol\":\"" + symbol + "\",\"period\":\"" + period + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_GETCANDLEDATA);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_GETCANDLEDATA + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static Response GetViopOrderHistory(string id, string Subaccount)
        {
            try
            {
                string postData = "{\"transactionId\":\"" + id + "\",\"subAccount\":\"" + Subaccount + "\"}";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_GETVIOPORDERHISTORY);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_GETVIOPORDERHISTORY + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Request: " + postData);

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<Response>(result);
            }
            catch (Exception ex)
            {
                return new Response() { Success = false, Message = ex.Message, Content = ex };
            }
        }

        public static bool SessionRefresh()
        {
            try
            {
                string postData = "";

                string result = string.Empty;

                var request = (HttpWebRequest)WebRequest.Create(apiurl + URL_SESSIONREFRESH);

                var data = Encoding.UTF8.GetBytes(postData);

                request.Headers.Add("APIKEY", APIKEY);
                request.Headers.Add("Authorization", HASH);
                request.Headers.Add("Checker", ComputeSha256Hash(APIKEY + hostname + URL_SESSIONREFRESH + postData));
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.Accept = "application/json; charset=utf-8";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return DeserializeJson<bool>(result);
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string OpenSSLEncryptApi(string plainText, string apikey)
        {
            TripleDESCryptoServiceProvider keys = new TripleDESCryptoServiceProvider();
            keys.GenerateIV();
            keys.GenerateKey();
            string key = apikey;
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static T DeserializeJson<T>(string obj)
        {
            try
            {
                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                return JsonConvert.DeserializeObject<T>(obj, jsonSerializerSettings);
            }
            catch
            {
                return default(T);
            }
        }
        public static string SerializeJson(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return null;
            }
        }
    }
}
