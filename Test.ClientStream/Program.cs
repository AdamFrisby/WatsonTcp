﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WatsonTcp;

namespace TestClientStream
{
    internal class TestClientStream
    {
        private static string serverIp = "";
        private static int serverPort = 0;
        private static bool useSsl = false;
        private static string certFile = "";
        private static string certPass = "";
        private static bool acceptInvalidCerts = true;
        private static bool mutualAuthentication = true;
        private static WatsonTcpClient<BlankMetadata> client = null;
        private static string presharedKey = null;

        private static void Main(string[] args)
        {
            serverIp = InputString("Server IP:", "127.0.0.1", false);
            serverPort = InputInteger("Server port:", 9000, true, false);
            useSsl = InputBoolean("Use SSL:", false);

            InitializeClient();

            bool runForever = true;
            BlankMetadata metadata;
            bool success;

            while (runForever)
            {
                Console.Write("Command [? for help]: ");
                string userInput = Console.ReadLine();
                byte[] data = null;
                MemoryStream ms = null; 

                if (String.IsNullOrEmpty(userInput))
                {
                    continue;
                }

                switch (userInput)
                {
                    case "?":
                        Console.WriteLine("Available commands:");
                        Console.WriteLine("  ?              help (this menu)");
                        Console.WriteLine("  q              quit");
                        Console.WriteLine("  cls            clear screen");
                        Console.WriteLine("  send           send message to server");
                        Console.WriteLine("  send md        send message with metadata to server");
                        Console.WriteLine("  sendasync      send message to server asynchronously");
                        Console.WriteLine("  sendasync md   send message with metadata to server asynchronously");
                        Console.WriteLine("  status         show if client connected");
                        Console.WriteLine("  dispose        dispose of the connection");
                        Console.WriteLine("  connect        connect to the server if not connected");
                        Console.WriteLine("  reconnect      disconnect if connected, then reconnect");
                        Console.WriteLine("  psk            set the preshared key");
                        Console.WriteLine("  auth           authenticate using the preshared key");
                        Console.WriteLine("  debug          enable/disable debug (currently " + client.DebugMessages + ")");
                        break;

                    case "q":
                        runForever = false;
                        break;

                    case "cls":
                        Console.Clear();
                        break;

                    case "send":
                        Console.Write("Data: ");
                        userInput = Console.ReadLine();
                        if (String.IsNullOrEmpty(userInput)) break;
                        data = Encoding.UTF8.GetBytes(userInput);
                        ms = new MemoryStream(data);
                        success = client.Send(data.Length, ms);
                        Console.WriteLine(success);
                        break;

                    case "send md":
                        metadata = InputDictionary();
                        Console.Write("Data: ");
                        userInput = Console.ReadLine();
                        if (String.IsNullOrEmpty(userInput)) break;
                        data = Encoding.UTF8.GetBytes(userInput);
                        ms = new MemoryStream(data);
                        success = client.Send(metadata, data.Length, ms);
                        Console.WriteLine(success);
                        break;

                    case "sendasync":
                        Console.Write("Data: ");
                        userInput = Console.ReadLine();
                        if (String.IsNullOrEmpty(userInput)) break;
                        data = Encoding.UTF8.GetBytes(userInput);
                        ms = new MemoryStream(data);
                        success = client.SendAsync(data.Length, ms).Result;
                        Console.WriteLine(success);
                        break;

                    case "sendasync md":
                        metadata = InputDictionary();
                        Console.Write("Data: ");
                        userInput = Console.ReadLine();
                        if (String.IsNullOrEmpty(userInput)) break;
                        data = Encoding.UTF8.GetBytes(userInput);
                        ms = new MemoryStream(data);
                        success = client.SendAsync(metadata, data.Length, ms).Result;
                        Console.WriteLine(success);
                        break;

                    case "status":
                        if (client == null)
                        {
                            Console.WriteLine("Connected: False (null)");
                        }
                        else
                        {
                            Console.WriteLine("Connected: " + client.Connected);
                        }

                        break;

                    case "dispose":
                        client.Dispose();
                        break;

                    case "connect":
                        if (client != null && client.Connected)
                        {
                            Console.WriteLine("Already connected");
                        }
                        else
                        {
                            client = new WatsonTcpClient<BlankMetadata>(serverIp, serverPort);
                            client.ServerConnected += ServerConnected;
                            client.ServerDisconnected += ServerDisconnected;
                            client.StreamReceived += StreamReceived;
                            client.Start();
                        }
                        break;

                    case "reconnect":
                        if (client != null) client.Dispose();
                        client = new WatsonTcpClient<BlankMetadata>(serverIp, serverPort);
                        client.ServerConnected += ServerConnected;
                        client.ServerDisconnected += ServerDisconnected;
                        client.StreamReceived += StreamReceived;
                        client.Start();
                        break;

                    case "psk":
                        presharedKey = InputString("Preshared key:", "1234567812345678", false);
                        break;

                    case "auth":
                        client.Authenticate(presharedKey);
                        break;

                    case "debug":
                        client.DebugMessages = !client.DebugMessages;
                        Console.WriteLine("Debug set to: " + client.DebugMessages);
                        break;

                    default:
                        break;
                }
            }
        }

        private static void InitializeClient()
        {
            if (!useSsl)
            {
                client = new WatsonTcpClient<BlankMetadata>(serverIp, serverPort);
            }
            else
            {
                certFile = InputString("Certificate file:", "test.pfx", false);
                certPass = InputString("Certificate password:", "password", false);
                acceptInvalidCerts = InputBoolean("Accept Invalid Certs:", true);
                mutualAuthentication = InputBoolean("Mutually authenticate:", true);

                client = new WatsonTcpClient<BlankMetadata>(serverIp, serverPort, certFile, certPass);
                client.AcceptInvalidCertificates = acceptInvalidCerts;
                client.MutuallyAuthenticate = mutualAuthentication;
            }

            client.AuthenticationFailure += AuthenticationFailure;
            client.AuthenticationRequested = AuthenticationRequested;
            client.AuthenticationSucceeded += AuthenticationSucceeded;
            client.ServerConnected += ServerConnected;
            client.ServerDisconnected += ServerDisconnected;
            client.StreamReceived += StreamReceived;
            client.Logger = Logger;
            // client.Debug = true;
            client.Start();
        }

        private static bool InputBoolean(string question, bool yesDefault)
        {
            Console.Write(question);

            if (yesDefault) Console.Write(" [Y/n]? ");
            else Console.Write(" [y/N]? ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                if (yesDefault) return true;
                return false;
            }

            userInput = userInput.ToLower();

            if (yesDefault)
            {
                if (
                    (String.Compare(userInput, "n") == 0)
                    || (String.Compare(userInput, "no") == 0)
                   )
                {
                    return false;
                }

                return true;
            }
            else
            {
                if (
                    (String.Compare(userInput, "y") == 0)
                    || (String.Compare(userInput, "yes") == 0)
                   )
                {
                    return true;
                }

                return false;
            }
        }

        private static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

        private static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
        {
            while (true)
            {
                Console.Write(question);
                Console.Write(" [" + defaultAnswer + "] ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    return defaultAnswer;
                }

                int ret = 0;
                if (!Int32.TryParse(userInput, out ret))
                {
                    Console.WriteLine("Please enter a valid integer.");
                    continue;
                }

                if (ret == 0)
                {
                    if (allowZero)
                    {
                        return 0;
                    }
                }

                if (ret < 0)
                {
                    if (positiveOnly)
                    {
                        Console.WriteLine("Please enter a value greater than zero.");
                        continue;
                    }
                }

                return ret;
            }
        }

        private static BlankMetadata InputDictionary()
        {
            Console.WriteLine("Build metadata, press ENTER on 'Key' to exit");

            return new BlankMetadata();
            // TODO: Reimplement me
        }

        private static void LogException(string method, Exception e)
        {
            Console.WriteLine("");
            Console.WriteLine("An exception was encountered.");
            Console.WriteLine("   Method        : " + method);
            Console.WriteLine("   Type          : " + e.GetType().ToString());
            Console.WriteLine("   Data          : " + e.Data);
            Console.WriteLine("   Inner         : " + e.InnerException);
            Console.WriteLine("   Message       : " + e.Message);
            Console.WriteLine("   Source        : " + e.Source);
            Console.WriteLine("   StackTrace    : " + e.StackTrace);
            Console.WriteLine("");
        }
         
        private static void StreamReceived(object sender, StreamReceivedFromServerEventArgs<BlankMetadata> args) 
        {
            try
            {
                Console.Write("Stream from server [" + args.ContentLength + " bytes]: ");

                int bytesRead = 0;
                int bufferSize = 65536;
                byte[] buffer = new byte[bufferSize];
                long bytesRemaining = args.ContentLength;

                if (args.DataStream != null && args.DataStream.CanRead)
                {
                    while (bytesRemaining > 0)
                    {
                        bytesRead = args.DataStream.Read(buffer, 0, buffer.Length);
                        Console.WriteLine("Read " + bytesRead);

                        if (bytesRead > 0)
                        {
                            byte[] consoleBuffer = new byte[bytesRead];
                            Buffer.BlockCopy(buffer, 0, consoleBuffer, 0, bytesRead);
                            Console.Write(Encoding.UTF8.GetString(consoleBuffer));
                        }

                        bytesRemaining -= bytesRead;
                    }

                    Console.WriteLine("");
                }
                else
                {
                    Console.WriteLine("[null]");
                }
            }
            catch (Exception e)
            {
                LogException("StreamReceived", e);
            }
        }
         
        private static string AuthenticationRequested()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Server requests authentication");
            Console.WriteLine("Press ENTER and THEN enter your preshared key");
            if (String.IsNullOrEmpty(presharedKey)) presharedKey = InputString("Preshared key:", "1234567812345678", false);
            return presharedKey;
        }
         
        private static void AuthenticationSucceeded(object sender, EventArgs args) 
        {
            Console.WriteLine("Authentication succeeded");
        }
         
        private static void AuthenticationFailure(object sender, EventArgs args) 
        {
            Console.WriteLine("Authentication failed");
        }
         
        private static void ServerConnected(object sender, EventArgs args)
        {
            Console.WriteLine("Server connected");
        }

        private static void ServerDisconnected(object sender, EventArgs args)
        {
            Console.WriteLine("Server disconnected");
        } 

        private static void Logger(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}