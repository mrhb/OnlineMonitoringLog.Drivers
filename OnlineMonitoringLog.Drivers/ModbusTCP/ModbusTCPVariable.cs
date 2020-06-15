using System.Collections.Generic;
using AlarmBase.DomainModel.generics;
using OnlineMonitoringLog.Core.Interfaces;
using OnlineMonitoringLog.Core;

namespace OnlineMonitoringLog.Drivers.IEC104
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

        public class ObjAddress
        {
            public const int InputWaterTemp = 1;
            public const int OutputWaterTemp = 2;
            public const int OilPress = 3;
            public const int AdvanceSpark = 4;
            public const int ValvePosition = 5;
            public const int ValveFlow = 6;
            public const int ExhaustTemp = 7;
            public const int ElecPower = 8;
            public const int ElecEnergy = 9;
            public const int WorkTime = 10;
            public const int frequency = 11;
            public const int PowerFactor = 12;
        }
    }

