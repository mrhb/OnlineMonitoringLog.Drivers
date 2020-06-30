// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

using System.Net;
using System.Threading;
using System.Linq;
using OnlineMonitoringLog.Core;
using OnlineMonitoringLog.Core.Interfaces;
using InfluxDB.Collector;
using EasyModbus;
using System.Net.Sockets;
using System.Text;
using OnlineMonitoringLog.Drivers.Types;

namespace OnlineMonitoringLog.Drivers.ModbusTCP
{
    public class ModbusTCPUnit : Unit
    {
        static Thread ConnectionThread;
        private Thread ReadDataThread;
        static List<ModbusTCPUnit> obj = new List<ModbusTCPUnit>();
        private ModbusClient _modbusClient;
        public ModbusClient modbusClient
        {
            get { return _modbusClient; }
            set
            {
                _modbusClient = value;
            }
        }
        static ModbusTCPUnit()
        {
            ConnectionThread = new Thread(() => StartListening());
            ConnectionThread.Name = "ModbusTCP" ;
            ConnectionThread.IsBackground = true;
         
        }

        public ModbusTCPUnit(int unitId, IPAddress ip):base(unitId,ip)
        {
            if (ConnectionThread.ThreadState != ThreadState.Running)
                try { ConnectionThread.Start(); } catch { }

            obj.Add(this);

            ReadDataThread = new Thread(() => ProcessUa());
            ReadDataThread.Name = "ModbusTCPReadData"+unitId.ToString();
            ReadDataThread.IsBackground = true;
            ReadDataThread.Start();

            //*******InfluxDb Init*********************
            Metrics.Collector = new CollectorConfiguration()
                 .Batch.AtInterval(TimeSpan.FromSeconds(5))
                 .WriteTo.InfluxDB("http://localhost:8086", "OnlineMonitoringDb")
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
                    while (true)
                    {

                        int[] readHoldingRegisters = modbusClient.ReadHoldingRegisters(0, 12);    //Read 10 Holding Registers from Server, starting with Address 1
                        var datas = new Dictionary<string, object>();
                        // Console Output
                        var randGen = new Random();
                        for (int i = 1; i < readHoldingRegisters.Length; i++)
                        {
                            Console.WriteLine($"Id:{modbusClient.UnitIdentifier}   Value of HoldingRegister " + (i + 1) + " " + readHoldingRegisters[i].ToString());

                            int val = readHoldingRegisters[i]+ randGen.Next(500);
                            var item = Variables.Where(p => ((ModbusTCPVariable)p).ObjectAddress == i).First();
                            if (item.RecievedData(val, DateTime.Now))
                            {
                            
                                datas.Add(item.name.ToString(), val);
                            }
                        }
                        //*******InfluxDb ********
                        var tags = new Dictionary<string, string>() {
                            { "UnitName", ID.ToString() },
                            { "Company", "TetaPower" }
                        };
                        Metrics.Write("ModbusLogger", datas, tags);

                        Thread.Sleep(1000);
                    }
                }
                catch (Exception c)
                {
                    Console.WriteLine($"Error Occured in \"ProcessUa\" at {ReadDataThread.Name}");
                }

                Thread.Sleep(2000);
            }
        }

        public override string ToString() { return "ModbusTCP: " + Ip.ToString(); }
             
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        
        public static void StartListening()
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

                    // Wait until a connection is made before continuing.  
                    Thread.Sleep(5000);
                    allDone.WaitOne();
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


            var modbusClient = new ModbusClient(handler);

            modbusClient.UnitIdentifier = Convert.ToByte(0);
            int unitId = modbusClient.ReportSlaveID();
            Console.WriteLine($"{handler.RemoteEndPoint} has UnitId:  " + unitId.ToString());

            try
            {
                obj.Where(a => a.ID == unitId).First().modbusClient = modbusClient;
            }
            catch
            {
                handler.Close();
                Console.WriteLine("AcceptCallback Error in  Recieved UnitId: " + unitId.ToString());
            }

        }

    }
}

