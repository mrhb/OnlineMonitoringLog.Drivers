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

namespace OnlineMonitoringLog.Drivers.IEC104
{

    public class IEC104Unit : Unit
    {
        Timer generationTimer;
        private Timer ConnectionTimer;
        
        public IEC104Unit(int unitId, IPAddress ip):base(unitId,ip)
        {
            generationTimer = new Timer(UpdateWithRandom, null, 0, 1000);
            //ConnectionTimer = new Timer(ConnectToIec104Server, null, 0, 5000);
            // ConnectToIec104Server(null);
        }

        private void UpdateWithRandom(object state)
        {
            var rnd = new Random();
          foreach ( var item in Variables)
            {
                item.RecievedData(rnd.Next(100), DateTime.Now);
              
            }
        }

        private void ConnectToIec104Server(object state)

        {
            Console.WriteLine("Connect to Iec104Server Using lib60870.NET version " + LibraryCommon.GetLibraryVersionString());
            Connection con = new Connection(Ip.ToString());

            con.DebugOutput = false;

            con.SetASDUReceivedHandler(asduReceivedHandler, null);
            con.SetConnectionHandler(ConnectionHandler, null);

            try
            {
                con.Connect();
                ConnectionTimer = null;
            }
            catch (Exception)
            {

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
               new IEC104Variable(ID,ObjAddress.InputWaterTemp,ObjAddress.InputWaterTemp.ToString(),repo),
               //new iec104Variable(ID,ObjAddress.OutputWaterTemp, "OutputWaterTemp",repo),
               //new iec104Variable(ID,ObjAddress.OilPress, "OilPress",repo),
               //new iec104Variable(ID,ObjAddress.AdvanceSpark, "AdvanceSpark",repo),
               //new iec104Variable(ID,ObjAddress.ValvePosition, "ValvePosition",repo),
               //new iec104Variable(ID,ObjAddress.ValveFlow, "ValveFlow",repo),
               //new iec104Variable(ID,ObjAddress.ExhaustTemp, "ExhaustTemp",repo),
               //new iec104Variable(ID,ObjAddress.ElecPower, "ElecPower",repo),
               //new iec104Variable(ID,ObjAddress.ElecEnergy, "ElecEnergy",repo),
               //new iec104Variable(ID,ObjAddress.WorkTime, "WorkTime",repo),
               //new iec104Variable(ID,ObjAddress.frequency, "frequency",repo),
               //new iec104Variable(ID,ObjAddress.PowerFactor, "PowerFactor",repo),
            };

            return resources;
        }

       


    }
   
}

