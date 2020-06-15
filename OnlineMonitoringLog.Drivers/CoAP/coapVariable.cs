// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;

using CoAP;

using OnlineMonitoringLog.Core;
using OnlineMonitoringLog.Core.Interfaces;
using AlarmBase.DomainModel.generics;

namespace OnlineMonitoringLog.Drivers.CoAP
{
    public class coapVariable : LoggableVariable<int> , ILoggableVariable<int>
    {
        CoapClient _CoapClient;
       public coapVariable(int unitId, IPAddress ip, string resourceName, ILoggRepository repo) : base(unitId, resourceName,repo)
        {
            _CoapClient = new CoapClient();
            name = resourceName;
            _CoapClient.Uri = new Uri("coap://" + ip.ToString() + "/" + resourceName);
            _CoapClient.ObserveAsync();
            _CoapClient.Respond += RecievedRespond;
        }
        private void RecievedRespond(object sender, ResponseEventArgs e)
        {
            value = e.Response.ResponseText;
            timeStamp = DateTime.Now;
        }
   

        public override string ToString()
        {
            return value;
        }

        public override List<Occurence<int>> ObjOcucrences()
        {
            throw new NotImplementedException();
        }
    }

}

