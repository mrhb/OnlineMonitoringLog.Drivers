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

namespace OnlineMonitoringLog.Drivers.IEC104
{
    public class ModbusTCPUnit : Unit
    {
        private Timer ConnectionTimer;
        private Thread t;

        public ModbusTCPUnit(int unitId, IPAddress ip):base(unitId,ip)
        {
            //ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);

            t = new Thread(() => ProcessUa());
            t.Name = "IEC104_" + unitId.ToString();
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

        private void ProcessUa()
        {
            Console.WriteLine("Connecting....in theread" + LibraryCommon.GetLibraryVersionString());
            Connection con = new Connection("127.0.0.1", 2404);//Ip.ToString());

            con.DebugOutput = false;

            con.SetASDUReceivedHandler(asduReceivedHandler, null);
            con.SetConnectionHandler(ConnectionHandler, null);

            try
            {
                con.Connect();
                ConnectionTimer = null;
            }
            catch (Exception C)
            {
                Console.WriteLine(C.Message);
                ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Connecting....in BackgroundWorker" + LibraryCommon.GetLibraryVersionString());
            Connection con = new Connection("127.0.0.1", 2404);//Ip.ToString());

            con.DebugOutput = false;

            con.SetASDUReceivedHandler(asduReceivedHandler, null);
            con.SetConnectionHandler(ConnectionHandler, null);

            try
            {
                con.Connect();
                ConnectionTimer = null;
            }
            catch (Exception C)
            {
                Console.WriteLine(C.Message);
                ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);
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
        
        public override string ToString() { return "IEC104: " + Ip.ToString(); }
       

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

                for (int i = 0; i < asdu.NumberOfElements; i++)
                {

                    var val = (MeasuredValueShortWithCP56Time2a)asdu.GetElement(i);
                    var item = Variables.Where(p => ((IEC104Variable)p).ObjectAddress == val.ObjectAddress).First();
                    item.RecievedData((int)val.Value, DateTime.Now);


                    Metrics.Increment("mrhb_iterations");
                    var datas = new Dictionary<string, object>
                    {
                         {item.name.ToString()+"_"+ID.ToString(), (int)val.Value}
                    };
                    Metrics.Write("UIWPF", datas);


                }
            }
            else
            {
                Console.WriteLine("Unknown message type!");
            }
           return true;
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

       


    }
   
}

