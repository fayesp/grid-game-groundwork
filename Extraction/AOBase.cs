using Common.Log;
using Common.RTSystem;
using IECommonEntiry.Entity;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTSystem
{
    public interface IAOBase
    {
        EventID StartEvent { get; set; }
        EventID EndEvent { get; set; }
        EventID AlarmEvent { get; set; }
        ECID TimeoutEC { get; set; }
        void AOOperate(int setValue);
        bool CheckAOOperate(int setValue);
        void AOOperate(double setValue);
        bool CheckAOOperate(double setValue);
        void SetIOAccessor(IIOAccessor IOAccessor);
    }
    internal class AOBase : IOBase, IAOBase
    {
        public EventID StartEvent { get; set; }
        public EventID EndEvent { get; set; }
        public EventID AlarmEvent { get; set; }
        public ECID TimeoutEC { get; set; }

        private IOID ao;
        public IOID AO { get => ao; }

        public AOBase(string AOName)
        {
            Init(AOName);
        }
        public AOBase(IOID AO,EventID startEvent,EventID endEvent,EventID alarmEvent,ECID timeoutEC)
        {
            ao = AO;
            StartEvent = startEvent;
            EndEvent = endEvent;
            AlarmEvent = alarmEvent;
            TimeoutEC = timeoutEC;
        }
        public void Init(string AOName)
        {
            ao = NameConVertIO("AO_" + AOName);
            StartEvent = NameConVertEvent("EV_" + AOName + "_START");
            EndEvent = NameConVertEvent("EV_" + AOName + "_CPLT");
            AlarmEvent = NameConVertEvent("AL_" + AOName + "TIMEOUT");
            TimeoutEC = ECID.EC_CYLINDER_OPERATION_TIMEOUT;
        }
        public void AOOperate(int setValue)
        {
            WriteIO(AO, setValue);
        }

        public bool CheckAOOperate(int setValue)
        {
            return ReadIAI(AO) == setValue;
        }

        public void AOOperate(double setValue)
        {
            WriteIO(AO, (float)Math.Round(setValue,2));
        }
        public bool CheckAOOperate(double setValue)
        {
            return Math.Round(ReadFAI(AO)) == Math.Round(setValue);
        }
    }

}
