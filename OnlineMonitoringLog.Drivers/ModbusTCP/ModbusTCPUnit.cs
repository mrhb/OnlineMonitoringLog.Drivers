// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

using System.Net;
using lib60870;
using lib60870.CS101;
using lib60870.CS104;
using System.Threading;
using System.Linq;
using OnlineMonitoringLog.Core;
using OnlineMonitoringLog.Core.Interfaces;
using System.ComponentModel;
using InfluxDB.Collector;
using EasyModbus;
using System.Net.Sockets;
using System.Text;
using OnlineMonitoringLog.Drivers.Types;

namespace OnlineMonitoringLog.Drivers.ModbusTCP
{
    public class ModbusTCPUnit : Unit
    {
        private Timer ConnectionTimer;
        private Thread t;

        public ModbusTCPUnit(int unitId, IPAddress ip):base(unitId,ip)
        {
            //ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);

            t = new Thread(() => StartListening());
            t.Name = "ModbusTCP" + unitId.ToString();
            t.IsBackground = true;
            t.Start();

            //*******InfluxDb Init*********************
            Metrics.Collector = new CollectorConfiguration()
                 .Tag.With("Company", "TetaPower")
                 .Tag.With("UnitName", unitId.ToString())
                 .Batch.AtInterval(TimeSpan.FromSeconds(2))
                 .WriteTo.InfluxDB("http://localhost:8086", "telegraf")
                 // .WriteTo.InfluxDB("udp://localhost:8089", "data")
                 .CreateCollector();
            //***************
        }
        public override List<ILoggableVariable<int>> UnitVariables()
        {
            var resources = new List<ILoggableVariable<int>>() {
               new ModbusTCPVariable(ID,ObjAddress.InputWaterTemp,"InputWaterTemp",repo),
               new ModbusTCPVariable(ID,ObjAddress.OutputWaterTemp, "OutputWaterTemp",repo),
               new ModbusTCPVariable(ID,ObjAddress.OilPress, "OilPress",repo),
               new ModbusTCPVariable(ID,ObjAddress.AdvanceSpark, "AdvanceSpark",repo),
               new ModbusTCPVariable(ID,ObjAddress.ValvePosition, "ValvePosition",repo),
               new ModbusTCPVariable(ID,ObjAddress.ValveFlow, "ValveFlow",repo),
               new ModbusTCPVariable(ID,ObjAddress.ExhaustTemp, "ExhaustTemp",repo),
               new ModbusTCPVariable(ID,ObjAddress.ElecPower, "ElecPower",repo),
               new ModbusTCPVariable(ID,ObjAddress.ElecEnergy, "ElecEnergy",repo),
               new ModbusTCPVariable(ID,ObjAddress.WorkTime, "WorkTime",repo),
               new ModbusTCPVariable(ID,ObjAddress.frequency, "frequency",repo),
               new ModbusTCPVariable(ID,ObjAddress.PowerFactor, "PowerFactor",repo),
            };

            return resources;
        }

        private void ProcessUa()
        {
            while (true)
            {
                try
                {
                    ModbusClient modbusClient = new ModbusClient("192.168.1.110", 502);    //Ip-Address and Port of Modbus-TCP-Server
                    modbusClient.Connect();                                                    //Connect to Server
                    modbusClient.WriteMultipleCoils(4, new bool[] { true, true, true, true, true, true, true, true, true, true });    //Write Coils starting with Address 5
                                                                                                                                      //bool[] readCoils = modbusClient.ReadCoils(9, 10);                        //Read 10 Coils from Server, starting with address 10
                    while (true)
                    {
                        int[] readHoldingRegisters = modbusClient.ReadHoldingRegisters(0, 12);    //Read 10 Holding Registers from Server, starting with Address 1
                        var datas = new Dictionary<string, object>();
                        // Console Output

                        for (int i = 1; i < readHoldingRegisters.Length; i++)
                        {
                            Console.WriteLine("Value of HoldingRegister " + (i + 1) + " " + readHoldingRegisters[i].ToString());
                            int val = readHoldingRegisters[i];
                            var item = Variables.Where(p => ((ModbusTCPVariable)p).ObjectAddress == i).First();
                            if (item.RecievedData(val, DateTime.Now))
                            {
                                Metrics.Increment("mrhb_iterations");
                                datas.Add(item.name.ToString() + "_" + ID.ToString(), val);
                            }
                        }
                        Metrics.Write("UIWPF", datas);

                        Thread.Sleep(1000);
                    }
                }
                catch (Exception c)
                {
                    Console.Write($"Error Occured in {t.Name}: {c.ToString()}");
                }
            }
           
           
        }

       private void ConnectToIec104Server(object state)

        {
            Console.WriteLine("Connect to Iec104Server Using lib60870.NET version " + LibraryCommon.GetLibraryVersionString());
            Connection con = new Connection("127.0.0.1", 2404);//Ip.ToString());

            con.DebugOutput = false;

            con.SetASDUReceivedHandler(asduReceivedHandler, null);
            con.SetConnectionHandler(ConnectionHandler, null);

            try
            {
                con.Connect();
                ConnectionTimer = null;
            }
            catch (Exception C )
            {
                Console.WriteLine(C.Message);
                ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);
            }

        }  
        
        public override string ToString() { return "ModbusTCP: " + Ip.ToString(); }
       

        private void ConnectionHandler(object parameter, ConnectionEvent connectionEvent)
        {
            switch (connectionEvent)
            {
                case ConnectionEvent.OPENED:
                    Console.WriteLine("Connected");
                    ConnectionTimer = null;
                    break;
                case ConnectionEvent.CLOSED:
                    Console.WriteLine("Connection closed");
                    ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);
                    break;
                case ConnectionEvent.STARTDT_CON_RECEIVED:
                    Console.WriteLine("STARTDT CON received");

                    break;
                case ConnectionEvent.STOPDT_CON_RECEIVED:
                    Console.WriteLine("STOPDT CON received");
                    break;
            }
        }

        private bool asduReceivedHandler(object parameter, ASDU asdu)
        {
            Console.WriteLine(asdu.ToString());

            if (asdu.TypeId == TypeID.M_ME_TF_1)
            {

                
            }
            else
            {
                Console.WriteLine("Unknown message type!");
            }
           return true;
        }


        public static ManualResetEvent allDone = new ManualResetEvent(false);



        public  void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("192.168.1.19");// ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 503);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
            
                    try
                    {

                        while (true)
                        {
                            int[] serverResponse = modbusClient.ReadHoldingRegisters(1, 10);

                            int[] readHoldingRegisters = modbusClient.ReadHoldingRegisters(0, 12);    //Read 10 Holding Registers from Server, starting with Address 1
                            var datas = new Dictionary<string, object>();
                            // Console Output

                            for (int i = 1; i < readHoldingRegisters.Length; i++)
                            {
                                Console.WriteLine("Value of HoldingRegister " + (i + 1) + " " + readHoldingRegisters[i].ToString());
                                int val = readHoldingRegisters[i];
                                var item = Variables.Where(p => ((ModbusTCPVariable)p).ObjectAddress == i).First();
                                if (item.RecievedData(val, DateTime.Now))
                                {
                                    Metrics.Increment("mrhb_iterations");
                                    datas.Add(item.name.ToString() + "_" + ID.ToString(), val);
                                }
                            }
                            Metrics.Write("UIWPF", datas);

                            Thread.Sleep(1000);
                        }
                    }
                    catch {

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();
                    }

                  
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            Console.WriteLine($"socket conncetion to {handler.RemoteEndPoint} has established");
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;

            for (int j = 1; j == 1; j++)
            {
                modbusClient = new ModbusClient(handler);
                // modbus.UnitIdentifier = Convert.ToByte(j);
                int[] serverResponse = modbusClient.ReadHoldingRegisters(1, 10);

                for (int i = 0; i < serverResponse.Length; i++)
                {
                    Console.WriteLine($"data from {handler.RemoteEndPoint} recieved: {serverResponse[i]} ");
                }

                Thread.Sleep(500);
                //    modbus.ReadHoldingRegisters(1, 10);
                try
                {
                }
                catch
                { Console.WriteLine("time out in  stream.Read"); }

            }

        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.  
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        static ModbusClient modbusClient;


    }
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }


}

