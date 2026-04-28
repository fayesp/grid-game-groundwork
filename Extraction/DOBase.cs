using IECommonEntiry.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTSystem
{
    public interface IDOBase
    {
        EventID StartEvent{get;set;}
        EventID EndEvent{get;set;}
        EventID AlarmEvent{get;set;}
        ECID TimeoutEC { get; set; }
        void DOOperate();
        bool DOOperateCheck();
        void CallBackResetIO();
        void SetIOAccessor(IIOAccessor IOAccessor);
    }

    /// <summary>
    /// Valve和Cylinder可以直接使用DOBase
    /// </summary>
    public class DOBase : IOBase,IDOBase
    {
        #region property
        public IOID do_True { get; private set; }
        public bool DO_TRUE
        {
            get { return ReadDI(do_True); }
            set { WriteIO(do_True, value); }
        }
        public IOID do_False { get; private set; }
        public bool DO_FALSE
        {
            get { return ReadDI(do_False); }
            set { WriteIO(do_False, value); }
        }
        private List<IOID> di_True = new List<IOID>();
        private List<IOID> di_False = new List<IOID>();
        public List<IOID> DI_TRUE { get => di_True; }
        public List<IOID> DI_FALSE { get => di_False; }
        public EventID StartEvent { get; set; }
        public EventID EndEvent { get; set; }
        public EventID AlarmEvent { get; set; }
        public ECID TimeoutEC { get; set; }


        #endregion property

        #region constructor
        public DOBase(IOID DO_True, IOID DO_False, List<IOID> DI_True, List<IOID> DI_False, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
        {
            do_True = DO_True;
            do_False = DO_False;
            di_True = DI_True;
            di_False = DI_False;
            StartEvent = startEvent;
            EndEvent = endEvent;
            AlarmEvent = alarmEvent;
            TimeoutEC = timeoutEC;
        }
        public DOBase(IOID DO_True, List<IOID> DI_True, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
        {
            do_True = DO_True;
            di_True = DI_True;
            StartEvent = startEvent;
            EndEvent = endEvent;
            AlarmEvent = alarmEvent;
            TimeoutEC = timeoutEC;
        }
        public DOBase(IOID DO_True, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
        {
            do_True = DO_True;
            StartEvent = startEvent;
            EndEvent = endEvent;
            AlarmEvent = alarmEvent;
            TimeoutEC = timeoutEC;
        }
        public DOBase(IOID DO_True,List<IOID> DI_True,List<IOID> DI_False,EventID startEvent ,EventID endEvent,EventID alarmEvent, ECID timeoutEC)
        {
            do_True = DO_True;
            do_False = default(IOID);
            di_True = DI_True;
            di_False = DI_False;
            StartEvent = startEvent;
            EndEvent = endEvent;
            AlarmEvent = alarmEvent;
            TimeoutEC = timeoutEC;
        }
        public DOBase(string DO_True, string DO_False, List<string> DI_On,
                List<string> DI_Off, string startEvent, string endEvent, string alarmEvent, string timeoutEC)
        {
            do_True = NameConVertIO(DO_True);
            do_False = NameConVertIO(DO_False);

            foreach (var item in DI_On)
            {
                di_True.Add(NameConVertIO(item));
            }
            foreach (var item in DI_Off)
            {
                di_False.Add(NameConVertIO(item));
            }
            StartEvent = NameConVertEvent(startEvent);
            EndEvent = NameConVertEvent(endEvent);
            AlarmEvent = NameConVertEvent(alarmEvent);
            TimeoutEC = NameConVertEC(timeoutEC);
        }
        public DOBase(string DO_True, string DI_On)
        {
            do_True = NameConVertIO(DO_True);
            di_True.Add(NameConVertIO(DI_On));
        }
        #endregion constructor

        #region fuction
        public virtual void DOOperate()
        {
            WriteDO(do_True, do_False);
        }
        public virtual bool DOOperateCheck()
        {
            return DI_TRUE.All(x =>ReadDI(x) == true) && DI_FALSE.All(x => ReadDI(x) == false);
        }

        public virtual void CallBackResetIO()
        {
            DO_TRUE = false;
            DO_FALSE = false;
        }
        #endregion fuction

    }



    public class AlarmCheckDO : DOBase
    {
        List<IOID> AI_TO_CHECK = new List<IOID>();

        public AlarmCheckDO(IOID DO_True, List<IOID> AIs, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC) 
            : base(DO_True, startEvent, endEvent, alarmEvent, timeoutEC)
        {
            AI_TO_CHECK = AIs;
        }

        public override void DOOperate()
        {
            DO_TRUE = true;
        }
        public override bool DOOperateCheck()
        {
            return  AI_TO_CHECK.All(x => ReadIAI(x) == 0);
        }
        public override void CallBackResetIO()
        {
            DO_TRUE = false;
        }
    }
    public class MotorDAO : DOBase
    {
        IOID AI_TO_CHECK;
        IOID AO_TO_CHECK;

        public MotorDAO(IOID DO_True,IOID AO,IOID AI, List<IOID> DI_On,
                List<IOID> DI_Off, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
            : base(DO_True, DI_On,
                 DI_Off, startEvent, endEvent, alarmEvent, timeoutEC)
        {
            AI_TO_CHECK = AI;
            AO_TO_CHECK = AO;
        }

        public override void DOOperate()
        {
            DO_TRUE = true;
        }
        public override bool DOOperateCheck()
        {
            return Math.Abs( ReadDAI(AI_TO_CHECK) - ReadDAI(AO_TO_CHECK))<0.01 && base.DOOperateCheck();
        }
        public override void CallBackResetIO()
        {
            DO_TRUE = false;
        }
    }
    public class MotorTwoDO : DOBase
    {

        public MotorTwoDO(IOID DO_True, IOID DO_False, List<IOID> DI_On,
                List<IOID> DI_Off, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
            : base( DO_True, DO_False,  DI_On,
                 DI_Off, startEvent,  endEvent,  alarmEvent,  timeoutEC)
        {
        }

        public override void DOOperate()
        {
            DO_FALSE = false;
            DO_TRUE = true;
        }
    }
    public class MotorVelDO : DOBase
    {
        public MotorVelDO(IOID DO_True, IOID DO_False, List<IOID> DI_On,
                List<IOID> DI_Off, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
            : base(DO_True, DO_False, DI_On,
                 DI_Off, startEvent, endEvent, alarmEvent, timeoutEC)
        {
        }
        public override void DOOperate()
        {
            DO_FALSE = false;
            DO_TRUE = true;
        }
        public override void CallBackResetIO()
        {

        }
    }
    public class MotorVelStopDO : DOBase
    {
        public MotorVelStopDO(IOID DO_True, IOID DO_False, List<IOID> DI_On,
                List<IOID> DI_Off, EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC)
            : base(DO_True, DO_False, DI_On,
                 DI_Off, startEvent, endEvent, alarmEvent, timeoutEC)
        {
        }
        public override void DOOperate()
        {
            DO_FALSE = false;
            DO_TRUE = false;
        }
    }
    public class MotorDO : DOBase
    {
        public MotorDO(IOID DO_True, List<IOID> DI_True, List<IOID> DI_False,EventID startEvent, EventID endEvent, EventID alarmEvent, ECID timeoutEC) 
            : base(DO_True, DI_True, DI_False, startEvent, endEvent, alarmEvent, timeoutEC)
        {
        }
        public override void DOOperate()
        {
            DO_TRUE = true;
        }
        public override void CallBackResetIO()
        {
            DO_TRUE = false;
        }
        //public override bool OperateDOCheck()
        //{
        //    return DI_TRUE.All(x => ReadDI(x) == true);
        //}
    }
}
