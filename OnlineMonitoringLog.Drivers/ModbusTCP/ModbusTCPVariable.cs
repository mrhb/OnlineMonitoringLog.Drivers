using System.Collections.Generic;
using AlarmBase.DomainModel.generics;
using OnlineMonitoringLog.Core.Interfaces;
using OnlineMonitoringLog.Core;

namespace OnlineMonitoringLog.Drivers.ModbusTCP
{
    public class ModbusTCPVariable : LoggableVariable<int>
    {

            int _ObjectAddress;
            public ModbusTCPVariable(int unitId, int ObjectAddress, string resourceName, ILoggRepository Repo) : base(unitId, resourceName, Repo)
            {
                name = resourceName;
                _ObjectAddress = ObjectAddress;
            }


            public int ObjectAddress { get { return _ObjectAddress; } }

        public override List<Occurence<int>> ObjOcucrences()
        {
          return new List<Occurence<int>>() { };
        }

        public override string ToString()
            {
                return value;
            }

        }

    
    }

