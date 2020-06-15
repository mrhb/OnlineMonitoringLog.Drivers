// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;

using System.Net;
using System.Runtime.CompilerServices;
using OnlineMonitoringLog.Core.Interfaces;
using OnlineMonitoringLog.Core;

namespace OnlineMonitoringLog.Drivers.CoAP
{
    public class coapUnit : Unit
    {
        public coapUnit(int unitId, IPAddress ip) : base(unitId,ip) { }       
        
        public override string ToString() { return "CoAP: " + Ip.ToString(); }

        public override List<ILoggableVariable<int>> UnitVariables()
        {
           var names= new List<string>() { "ServerTime", "TimeOfDay", "helloworld" };

            var resources = new List<ILoggableVariable<int>>();

            foreach (var res in names)
            {
                var Client = new coapVariable(ID,_Ip, res, repo);
                resources.Add(Client);
            }
            return resources;
        }
    }
}

