using Common.Log;
using IECommonEntiry.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTSystem
{

    public interface IValveBase
    {
        IOID DO_VALVE_ON { get; set; }
        IOID DO_VALVE_OFF { get; set; }
        IOID DI_VALVE_ON_RSP { get;}
        IOID DI_VALVE_OFF_RSP { get;}
        EventID ValveOnStart { get; set; }
        EventID ValveOnCplt { get; set; }
        EventID ValveOffStart { get; set; }
        EventID ValveOffCplt { get; set; }
        EventID ValveOnAlarm { get; set; }
        ECID ValveOperateTimeoutEC { get; set; }
        IDOBase ValveOn { get; set; }
        IDOBase ValveOff { get; set; }
        void SetIOAccessor(IIOAccessor IOAccessor);

    }
    public class Valve : IOBase,IValveBase
    {
        private string UnitName { get; set; }
        private string ValveName { get; set; }
        public IOID DO_VALVE_ON { get; set; }
        public IOID DO_VALVE_OFF{get;set;}
        public IOID DI_VALVE_ON_RSP{get;set;}
        public IOID DI_VALVE_OFF_RSP{get;set;}
        public EventID ValveOnStart { get; set; }
        public EventID ValveOnCplt { get; set; }
        public EventID ValveOffStart { get; set; }
        public EventID ValveOffCplt { get; set; }
        public EventID ValveOnAlarm { get; set; }
        public EventID ValveOffAlarm { get; set; }
        public ECID ValveOperateTimeoutEC { get; set; }
        public IDOBase ValveOn { get; set; }
        public IDOBase ValveOff { get; set; }

        public Valve(string IOName)
        {
            ValveName = IOName;
            if (Init())
            {
                ValveOn = new DOBase(DO_VALVE_ON, DO_VALVE_OFF,new List<IOID>{DI_VALVE_ON_RSP},new List<IOID> {DI_VALVE_OFF_RSP},ValveOnStart,ValveOnCplt,ValveOnAlarm,ValveOperateTimeoutEC);
                ValveOff = new DOBase(DO_VALVE_OFF, DO_VALVE_ON,new List<IOID>{DI_VALVE_OFF_RSP},new List<IOID> {DI_VALVE_ON_RSP},ValveOffStart,ValveOffCplt,ValveOffAlarm,ValveOperateTimeoutEC);
            }
        }
        public Valve(string unitname, string IOName)
        {
            UnitName = unitname;
            ValveName = IOName;
            if (Init())
            {
                ValveOn = new DOBase(DO_VALVE_ON, DO_VALVE_OFF, new List<IOID> { DI_VALVE_ON_RSP }, new List<IOID> { DI_VALVE_OFF_RSP }, ValveOnStart, ValveOnCplt, ValveOnAlarm, ValveOperateTimeoutEC);
                ValveOff = new DOBase(DO_VALVE_OFF, DO_VALVE_ON, new List<IOID> { DI_VALVE_OFF_RSP }, new List<IOID> { DI_VALVE_ON_RSP }, ValveOffStart, ValveOffCplt, ValveOffAlarm, ValveOperateTimeoutEC);
            }
        }
        public Valve(string unitname, string IOName, string DO_On, string DO_Off, string DI_On,
            string DI_Off)
        {
            UnitName = unitname;
            ValveName = IOName;
            if (Init(DO_On, DO_Off, DI_On, DI_Off))
            {
                ValveOn = new DOBase(DO_VALVE_ON, DO_VALVE_OFF, new List<IOID> { DI_VALVE_ON_RSP }, new List<IOID> { DI_VALVE_OFF_RSP }, ValveOnStart, ValveOnCplt, ValveOnAlarm, ValveOperateTimeoutEC);
                ValveOff = new DOBase(DO_VALVE_OFF, DO_VALVE_ON, new List<IOID> { DI_VALVE_OFF_RSP }, new List<IOID> { DI_VALVE_ON_RSP }, ValveOffStart, ValveOffCplt, ValveOffAlarm, ValveOperateTimeoutEC);
            }
        }
        public Valve(string Name, IIOAccessor IOAccessor)
        {
            ValveName = Name;
            _ioAccessor = IOAccessor;
            Init();
        }
        public override void SetIOAccessor(IIOAccessor IOAccessor)
        {
            _ioAccessor = IOAccessor;
            if (ValveOn != null)
            {
                ValveOn.SetIOAccessor(IOAccessor);
            }
            if (ValveOff != null)
            {
                ValveOff.SetIOAccessor(IOAccessor);
            } 
        }
        private bool Init()
        {
            try
            {
                string[] strings = ValveName.Split('_');
                string evValveName = ValveName;
                if (strings.Length > 0 && !string.IsNullOrEmpty(UnitName) && strings[0] != UnitName.ToUpper())
                {
                    strings[0] = UnitName.ToUpper();
                    evValveName = string.Join("_", strings);
                }

                DO_VALVE_ON = NameConVertIO("DO_" + ValveName + "_ON");
                DO_VALVE_OFF = NameConVertIO("DO_" + ValveName + "_OFF");
                DI_VALVE_ON_RSP = NameConVertIO("DI_" + ValveName + "_ON_RSP");
                DI_VALVE_OFF_RSP = NameConVertIO("DI_" + ValveName + "_OFF_RSP");
                
                ValveOnStart = NameConVertEvent("EV_" + evValveName + "_ON_START");
                ValveOnCplt = NameConVertEvent("EV_" + evValveName + "_ON_CPLT");
                ValveOffStart = NameConVertEvent("EV_" + evValveName + "_OFF_START");
                ValveOffCplt = NameConVertEvent("EV_" + evValveName + "_OFF_CPLT");
                ValveOnAlarm = NameConVertEvent("AL_" + evValveName + "_ON_TIMEOUT");
                ValveOffAlarm = NameConVertEvent("AL_" + evValveName + "_OFF_TIMEOUT");
                ValveOperateTimeoutEC = ECID.EC_VALVE_OPERATION_TIMEOUT;
                return true;
            }
            catch (Exception e)
            {
                Logger.Instance.WriteIOInfoLog($"ValveBase {ValveName} Init Error: {e}");
                return false;
            }
        }
        private bool Init(string DO_On, string DO_Off, string DI_On,
            string DI_Off)
        {
            try
            {
                string[] strings = ValveName.Split('_');
                string evValveName = ValveName;
                if (strings.Length > 0 && !string.IsNullOrEmpty(UnitName) && strings[0] != UnitName.ToUpper())
                {
                    strings[0] = UnitName.ToUpper();
                    evValveName = string.Join("_", strings);
                }

                DO_VALVE_ON = NameConVertIO("DO_" + DO_On);
                DO_VALVE_OFF = NameConVertIO("DO_" + DO_Off);
                DI_VALVE_ON_RSP = NameConVertIO("DI_" + DI_On);
                DI_VALVE_OFF_RSP = NameConVertIO("DI_" + DI_Off);

                ValveOnStart = NameConVertEvent("EV_" + evValveName + "_ON_START");
                ValveOnCplt = NameConVertEvent("EV_" + evValveName + "_ON_CPLT");
                ValveOffStart = NameConVertEvent("EV_" + evValveName + "_OFF_START");
                ValveOffCplt = NameConVertEvent("EV_" + evValveName + "_OFF_CPLT");
                ValveOnAlarm = NameConVertEvent("AL_" + evValveName + "_ON_TIMEOUT");
                ValveOffAlarm = NameConVertEvent("AL_" + evValveName + "_OFF_TIMEOUT");
                ValveOperateTimeoutEC = ECID.EC_VALVE_OPERATION_TIMEOUT;
                return true;
            }
            catch (Exception e)
            {
                Logger.Instance.WriteIOInfoLog($"ValveBase {ValveName} Init Error: {e}");
                return false;
            }
        }
    }
}
