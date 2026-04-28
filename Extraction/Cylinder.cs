using Common.Log;
using Common.RTSystem;
using IECommonEntiry.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTSystem
{
    public interface ICylinderBase
    {
        IOID DO_CYLINDER_ON { get; set;}
        IOID DO_CYLINDER_OFF { get; set; }
        List<IOID> DI_CYLINDER_ON { get; set;}
        List<IOID> DI_CYLINDER_OFF { get; set;}
        EventID CylinderOnStart { get; set; }
        EventID CylinderOnCplt { get; set; }
        EventID CylinderOffStart { get; set; }
        EventID CylinderOffCplt { get; set; }
        EventID CylinderOnAlarm { get; set; }
        ECID CylinderOperateTimeoutEC { get; set; }
        IDOBase CylinderOn { get; set; }
        IDOBase CylinderOff { get; set; }
        void SetIOAccessor(IIOAccessor IOAccessor);
    }


    public class Cylinder : IOBase, ICylinderBase
    {
        private string UnitName { get; set; }
        private string CylinderName { get; set; }
        public IOID DO_CYLINDER_ON { get; set; }
        public IOID DO_CYLINDER_OFF { get; set; }
        public List<IOID> DI_CYLINDER_ON { get; set; } = new List<IOID>();
        public List<IOID> DI_CYLINDER_OFF { get; set; } = new List<IOID>();
        public EventID CylinderOnStart { get; set; }
        public EventID CylinderOnCplt { get; set; }
        public EventID CylinderOffStart { get; set; }
        public EventID CylinderOffCplt { get; set; }
        public EventID CylinderOnAlarm { get; set; }
        public EventID CylinderOffAlarm { get; set; }
        public ECID CylinderOperateTimeoutEC { get; set; }
        public IDOBase CylinderOn { get; set; }
        public IDOBase CylinderOff { get; set; }

        public Cylinder(string CyName)
        {
            CylinderName = CyName;
            Init();
        }
        public Cylinder(string CyName, IIOAccessor IOAccessor)
        {
            CylinderName = CyName;
            _ioAccessor = IOAccessor;
            Init();
        }
        public Cylinder(string DO_On, string DO_Off, List<string> DI_On,
            List<string> DI_Off)
        {
            if (Init( DO_On, DO_Off, DI_On, DI_Off))
            {
                CylinderOn = new DOBase(DO_CYLINDER_ON, DO_CYLINDER_OFF,
                    DI_CYLINDER_ON,DI_CYLINDER_OFF ,
                    CylinderOnStart, CylinderOnCplt, CylinderOnAlarm, CylinderOperateTimeoutEC);
                CylinderOff = new DOBase(DO_CYLINDER_OFF, DO_CYLINDER_ON,
                    DI_CYLINDER_OFF ,DI_CYLINDER_ON ,
                    CylinderOffStart, CylinderOffCplt, CylinderOffAlarm, CylinderOperateTimeoutEC);
            }
        }
        public Cylinder(string unitname, string CyName,string DO_On, string DO_Off, List<string> DI_On,
            List<string> DI_Off)
        {
            UnitName = unitname;
            CylinderName = CyName;
            if (Init( DO_On, DO_Off, DI_On, DI_Off))
            {
                CylinderOn = new DOBase(DO_CYLINDER_ON, DO_CYLINDER_OFF,
                    DI_CYLINDER_ON,DI_CYLINDER_OFF ,
                    CylinderOnStart, CylinderOnCplt, CylinderOnAlarm, CylinderOperateTimeoutEC);
                CylinderOff = new DOBase(DO_CYLINDER_OFF, DO_CYLINDER_ON,
                    DI_CYLINDER_OFF ,DI_CYLINDER_ON ,
                    CylinderOffStart, CylinderOffCplt, CylinderOffAlarm, CylinderOperateTimeoutEC);
            }
        }
        public Cylinder(string CyName,string DO_On, string DO_Off)
        {
            CylinderName = CyName;
            if (Init( DO_On, DO_Off))
            {
                CylinderOn = new DOBase(DO_CYLINDER_ON, DO_CYLINDER_OFF,
                    DI_CYLINDER_ON,DI_CYLINDER_OFF ,
                    CylinderOnStart, CylinderOnCplt, CylinderOnAlarm, CylinderOperateTimeoutEC);
                CylinderOff = new DOBase(DO_CYLINDER_OFF, DO_CYLINDER_ON,
                    DI_CYLINDER_OFF ,DI_CYLINDER_ON ,
                    CylinderOffStart, CylinderOffCplt, CylinderOffAlarm, CylinderOperateTimeoutEC);
            }
        }
        public Cylinder(string unitname, string CyName, string DO_On, string DO_Off)
        {
            UnitName = unitname;
            CylinderName = CyName;
            if (Init(DO_On, DO_Off))
            {
                CylinderOn = new DOBase(DO_CYLINDER_ON, DO_CYLINDER_OFF,
                    DI_CYLINDER_ON, DI_CYLINDER_OFF,
                    CylinderOnStart, CylinderOnCplt, CylinderOnAlarm, CylinderOperateTimeoutEC);
                CylinderOff = new DOBase(DO_CYLINDER_OFF, DO_CYLINDER_ON,
                    DI_CYLINDER_OFF, DI_CYLINDER_ON,
                    CylinderOffStart, CylinderOffCplt, CylinderOffAlarm, CylinderOperateTimeoutEC);
            }
        }

        public override void SetIOAccessor(IIOAccessor IOAccessor)
        {
            _ioAccessor = IOAccessor;
            if (CylinderOn != null)
            {
                CylinderOn.SetIOAccessor(IOAccessor);
            }
            if (CylinderOff != null)
            {
                CylinderOff.SetIOAccessor(IOAccessor);
            }
        }
        private void Init()
        {

        }
        /// <summary>
        /// CylinderBase的初始化方法,需要传入开关气缸的DO和DI,以及相关的事件和超时EC
        /// Valve和Cylinder的初始化方法类似,但传入得参数不同,Valve的命名相对规范,只需要一个ValveName
        /// </summary>
        /// <param name="DO_On"></param>
        /// <param name="DO_Off"></param>
        /// <param name="DI_On"></param>
        /// <param name="DI_Off"></param>
        /// <returns></returns>
        private bool Init(string DO_On, string DO_Off, List<string> DI_On,
            List<string> DI_Off)
        {
            try
            {
                DO_CYLINDER_ON = NameConVertIO("DO_" + DO_On);
                DO_CYLINDER_OFF = NameConVertIO("DO_" + DO_Off);
                foreach (var DI in DI_On)
                {
                    DI_CYLINDER_ON.Add(NameConVertIO("DI_" + DI));
                }
                foreach (var DI in DI_Off)
                {
                    DI_CYLINDER_OFF.Add(NameConVertIO("DI_" + DI));
                }
                string[] Onstrings = DO_On.Split('_');
                string evDO_On = DO_On;
                if (Onstrings.Length > 0 && !string.IsNullOrEmpty(UnitName) && Onstrings[0] != UnitName.ToUpper())
                {
                    Onstrings[0] = UnitName.ToUpper();
                    evDO_On = string.Join("_", Onstrings);
                }
                string[] Offstrings = DO_Off.Split('_');
                string evDO_Off = DO_Off;
                if (Offstrings.Length > 0 && !string.IsNullOrEmpty(UnitName) && Offstrings[0] != UnitName.ToUpper())
                {
                    Offstrings[0] = UnitName.ToUpper();
                    evDO_Off = string.Join("_", Offstrings);
                }
                CylinderOnStart = NameConVertEvent("EV_" + evDO_On + "_START");
                CylinderOnCplt = NameConVertEvent("EV_" + evDO_On + "_CPLT");
                CylinderOffStart = NameConVertEvent("EV_" + evDO_Off + "_START");
                CylinderOffCplt = NameConVertEvent("EV_" + evDO_Off + "_CPLT");
                //TODO ALARM需要分两个,一个开一个关?TimeoutEC暂定,timeoutEC可以共用一个
                CylinderOnAlarm = NameConVertEvent("AL_" + evDO_On + "_TIMEOUT");
                CylinderOffAlarm = NameConVertEvent("AL_" + evDO_Off + "_TIMEOUT");
                CylinderOperateTimeoutEC = ECID.EC_CYLINDER_OPERATION_TIMEOUT;
                return true;
            }
            catch (Exception e)
            {
                Logger.Instance.WriteIOInfoLog($"ValveBase {CylinderName} Init Error: {e}");
                return false;
            }
        }
        private bool Init(string DO_On, string DO_Off)
        {
            try
            {
                string[] Onstrings = DO_On.Split('_');
                string evDO_On = DO_On;
                if (Onstrings.Length > 0 && !string.IsNullOrEmpty(UnitName) && Onstrings[0] != UnitName.ToUpper())
                {
                    Onstrings[0] = UnitName.ToUpper();
                    evDO_On = string.Join("_", Onstrings);
                }
                string[] Offstrings = DO_Off.Split('_');
                string evDO_Off = DO_Off;
                if (Offstrings.Length > 0 && !string.IsNullOrEmpty(UnitName) && Offstrings[0] != UnitName.ToUpper())
                {
                    Offstrings[0] = UnitName.ToUpper();
                    evDO_Off = string.Join("_", Offstrings);
                }
                DO_CYLINDER_ON = NameConVertIO("DO_" + DO_On);
                DO_CYLINDER_OFF = NameConVertIO("DO_" + DO_Off);
                DI_CYLINDER_ON.Add(NameConVertIO("DI_" + DO_On + "_RSP"));
                DI_CYLINDER_OFF.Add(NameConVertIO("DI_" + DO_Off + "_RSP"));
                DI_CYLINDER_ON.Add(NameConVertIO("DI_" + DO_On + "_SENSOR"));
                DI_CYLINDER_OFF.Add(NameConVertIO("DI_" + DO_Off + "_SENSOR"));
                CylinderOnStart = NameConVertEvent("EV_" + evDO_On + "_START");
                CylinderOnCplt = NameConVertEvent("EV_" + evDO_On + "_CPLT");
                CylinderOffStart = NameConVertEvent("EV_" + evDO_Off + "_START");
                CylinderOffCplt = NameConVertEvent("EV_" + evDO_Off + "_CPLT");
                //TODO ALARM需要分两个,一个开一个关?TimeoutEC暂定,timeoutEC可以共用一个
                CylinderOnAlarm = NameConVertEvent("AL_" + evDO_On + "_TIMEOUT");
                CylinderOffAlarm = NameConVertEvent("AL_" + evDO_Off + "_TIMEOUT");
                CylinderOperateTimeoutEC = ECID.EC_CYLINDER_OPERATION_TIMEOUT;
                return true;
            }
            catch (Exception e)
            {
                Logger.Instance.WriteIOInfoLog($"ValveBase {CylinderName} Init Error: {e}");
                return false;
            }
        }

    }
}
