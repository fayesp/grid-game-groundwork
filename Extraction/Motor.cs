using Common.Log;
using Common.RTSystem;
using IECommonEntiry.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTSystem
{
    public interface IMotorBase
    {
        string MotorName { get; set; }
        #region DO/DI
        IOID DO_TO_TEACHPOS { get; set; }
        IOID DO_ALARM_RESET { get; set; }
        IOID DO_DISABLE { get; set; }
        IOID DO_ENABLE { get; set; }
        IOID DO_HOME_START { get; set; }
        IOID DO_VEL_BKWD { get; set; }
        IOID DO_VEL_FRWD { get; set; }
        IOID DO_STOP { get; set; }
        IOID DO_JOG_BKWD { get; set; }
        IOID DO_JOG_FRWD { get; set; }
        IOID DO_TO_TARGETPOS { get; set; }
        IOID DI_AT_TEACHPOS { get; set;}
        IOID DI_VEL_REACHED { get; set;}
        IOID DI_DIR_BKWD { get; set;}
        IOID DI_DIR_FRWD { get; set;}
        IOID DI_DISABLED { get; set;}
        IOID DI_ENABLED { get; set;}
        IOID DI_HOME_CPLT { get; set;}
        IOID DI_MOVING { get; set;}
        IOID DI_NEGITIVE_LIMIT { get; set;}
        IOID DI_POSITIVE_LIMIT { get; set;}
        IOID DI_AT_TARGET_CPLT { get; set; }
        IOID DI_Z2_HOME_SENSOR { get; set; }

        #endregion DO/DI

        #region AO/AI
        IOID AO_ACC_SET { get; set; }
        IOID AO_DEC_SET { get; set; }
        IOID AO_POS_SET { get; set; }
        IOID AO_VEL_SET { get; set; }
        IOID AO_JOG_DISTANCE { get; set; }
        IOID AO_TO_TARGET_SET { get; set; }
        IOID AI_POS { get;set;}
        IOID AI_TRQ { get;set;}
        IOID AI_VEL { get;set;}
        IOID AI_AXIS_ALRAM_CODE { get;set;}
        IOID AI_STEP_ERROR_CODE { get;set;}
        IOID AI_AT_TARGET_POS { get; set; }
        #endregion AO/AI

        #region Event

        EventID ToTeachStart { get; set;}
        EventID ToTeachCPLT { get; set; }
        EventID ToTeachTimeout { get; set; }
        EventID AlarmResetStart { get; set; }
        EventID AlarmResetCPLT { get; set; }
        EventID AlarmResetTimeout { get; set; }
        EventID DisableStart { get; set; }
        EventID DisableCPLT { get; set; }
        EventID DisableTimeout { get; set; }
        EventID EnableStart { get; set; }
        EventID EnableCPLT { get; set; }
        EventID EnableTimeout { get; set; }
        EventID HomeStart { get; set; }
        EventID HomeCPLT { get; set; }
        EventID HomeTimeout { get; set; }
        EventID VelFRWDStart { get; set; }
        EventID VelFRWDCPLT { get; set; }
        EventID VelFRWDTimeout { get; set; }
        EventID VelBKWDStart { get; set; }
        EventID VelBKWDCPLT { get; set; }
        EventID VelBKWDTimeout { get;set; }
        EventID StopStart { get; set; }
        EventID StopCPLT { get; set; }
        EventID StopTimeout { get; set; }
        EventID JogFRWDStart { get; set; }
        EventID JogFRWDCPLT { get; set; }
        EventID JogFRWDTimeout { get; set; }
        EventID JogBKWDStart { get; set; }
        EventID JogBKWDCPLT { get; set; }
        EventID JogBKWDTimeout { get;set;}
        EventID ToTargetStart { get; set; }
        EventID ToTargetCPLT { get; set; }
        EventID ToTargetTimeout { get; set; }
        EventID ACCSetStart { get; set; }
        EventID ACCSetCPLT { get; set; }
        EventID ACCSetTimeout { get; set; }
        EventID DECSetStart { get; set; }
        EventID DECSetCPLT { get; set; }
        EventID DECSetTimeout { get; set; }
        EventID PosSetStart { get; set; }
        EventID PosSetCPLT { get; set; }
        EventID PosSetTimeout { get; set; }
        EventID VelSetStart { get; set; }
        EventID VelSetCPLT { get; set; }
        EventID VelSetTimeout { get; set; }
        EventID JogDistanceSetStart { get; set; }
        EventID JogDistanceSetCPLT { get; set; }
        EventID JogDistanceSetTimeout { get;set; }
        EventID ToTargetSetStart { get; set; }
        EventID ToTargetSetCPLT { get; set; }
        EventID ToTargetSetTimeout { get; set; }

        #endregion Event

        #region DOBASE

        IDOBase AlarmReset { get; set; }
        IDOBase Disable { get; set; }
        IDOBase Enable{ get; set; }
        IDOBase Home { get; set; }
        IDOBase JogFRWD { get; set; }
        IDOBase JogBKWD { get; set; }
        IDOBase Stop{ get; set; }
        IDOBase ToTargetPos { get; set; }
        IDOBase ToTeachPos { get; set; }
        IDOBase VelBKWD { get; set; }
        IDOBase VelFRWD { get; set; }
        IDOBase VelStop { get; set; }
        #endregion DOBASE

        #region AOBASE
        IAOBase ACCSet { get; set; }
        IAOBase DECSet { get; set; }
        IAOBase JogSet { get; set; }
        IAOBase PosSet { get; set; }
        IAOBase TargetSet { get; set; }
        IAOBase VelSet { get; set; }
        #endregion AOBASE
        void SetIOAccessor(IIOAccessor IOAccessor);
    }

    public class Motor:IOBase, IMotorBase
    {
        #region property
        private string UnitName { get; set; }
        public string MotorName { get; set; }

        #region DO/DI
        public IOID DO_TO_TEACHPOS { get; set; }
        public IOID DO_ALARM_RESET { get; set; }
        public IOID DO_DISABLE { get; set; }
        public IOID DO_ENABLE { get; set; }
        public IOID DO_HOME_START { get; set; }
        public IOID DO_VEL_BKWD { get; set; }
        public IOID DO_VEL_FRWD { get; set; }
        public IOID DO_STOP { get; set; }
        public IOID DO_JOG_BKWD { get; set; }
        public IOID DO_JOG_FRWD { get; set; }
        public IOID DO_TO_TARGETPOS { get; set; }

        public IOID DI_AT_TEACHPOS { get; set;}
        public IOID DI_VEL_REACHED { get; set;}
        public IOID DI_DIR_BKWD { get; set;}
        public IOID DI_DIR_FRWD { get; set;}
        public IOID DI_DISABLED { get; set;}
        public IOID DI_ENABLED { get; set;}
        public IOID DI_HOME_CPLT { get; set;}
        public IOID DI_MOVING { get; set;}
        public IOID DI_NEGITIVE_LIMIT { get; set;}
        public IOID DI_POSITIVE_LIMIT { get; set;}
        public IOID DI_AT_TARGET_CPLT { get; set; }
        public IOID DI_Z2_HOME_SENSOR { get; set; }


        #endregion DO/DI

        #region AO/AI
        public IOID AO_ACC_SET { get; set; }
        public IOID AO_DEC_SET { get; set; }
        public IOID AO_POS_SET { get; set; }
        public IOID AO_VEL_SET { get; set; }
        public IOID AO_JOG_DISTANCE { get; set; }
        public IOID AO_TO_TARGET_SET { get; set; }
        public IOID AI_POS { get; set;}
        public IOID AI_TRQ { get; set;}
        public IOID AI_VEL { get; set; }
        public IOID AI_AXIS_ALRAM_CODE { get;set;}
        public IOID AI_STEP_ERROR_CODE { get; set;}
        public IOID AI_AT_TARGET_POS { get; set; }

        #endregion AO/AI

        #region Event
        public EventID ToTeachStart { get; set; }
        public EventID ToTeachCPLT { get; set; }
        public EventID ToTeachTimeout { get; set; }
        public EventID AlarmResetStart { get; set; }
        public EventID AlarmResetCPLT { get; set; }
        public EventID AlarmResetTimeout { get; set; }
        public EventID DisableStart { get; set; }
        public EventID DisableCPLT { get; set; }
        public EventID DisableTimeout { get; set; }
        public EventID EnableStart { get; set; }
        public EventID EnableCPLT { get; set; }
        public EventID EnableTimeout { get; set; }
        public EventID HomeStart { get; set; }
        public EventID HomeCPLT { get; set; }
        public EventID HomeTimeout { get; set; }
        public EventID VelFRWDStart { get; set; }
        public EventID VelFRWDCPLT { get; set; }
        public EventID VelFRWDTimeout { get; set; }
        public EventID VelStopStart { get; set; }
        public EventID VelStopCPLT { get; set; }
        public EventID VelStopTimeout { get; set; }
        public EventID VelBKWDStart { get; set; }
        public EventID VelBKWDCPLT { get; set; }
        public EventID VelBKWDTimeout { get; set; }
        public EventID StopStart { get; set; }
        public EventID StopCPLT { get; set; }
        public EventID StopTimeout { get; set; }
        public EventID JogFRWDStart { get; set; }
        public EventID JogFRWDCPLT { get; set; }
        public EventID JogFRWDTimeout { get; set; }
        public EventID JogBKWDStart { get; set; }
        public EventID JogBKWDCPLT { get; set; }
        public EventID JogBKWDTimeout { get; set; }
        public EventID ToTargetStart { get; set; }
        public EventID ToTargetCPLT { get; set; }
        public EventID ToTargetTimeout { get; set; }
        public EventID ACCSetStart { get; set; }
        public EventID ACCSetCPLT { get; set; }
        public EventID ACCSetTimeout { get; set; }
        public EventID DECSetStart { get; set; }
        public EventID DECSetCPLT { get; set; }
        public EventID DECSetTimeout { get; set; }
        public EventID PosSetStart { get; set; }
        public EventID PosSetCPLT { get; set; }
        public EventID PosSetTimeout { get; set; }
        public EventID VelSetStart { get; set; }
        public EventID VelSetCPLT { get; set; }
        public EventID VelSetTimeout { get; set; }
        public EventID JogDistanceSetStart { get; set; }
        public EventID JogDistanceSetCPLT { get; set; }
        public EventID JogDistanceSetTimeout { get; set; }
        public EventID ToTargetSetStart { get; set; }
        public EventID ToTargetSetCPLT { get; set; }
        public EventID ToTargetSetTimeout { get; set; }
        #endregion Event

        #region DOBASE
        public IDOBase ToTeachPos { get; set; }
        public IDOBase AlarmReset { get; set; }
        public IDOBase Disable { get; set; }
        public IDOBase Enable { get; set; }
        public IDOBase Home { get; set; }
        public IDOBase VelBKWD { get; set; }
        public IDOBase VelFRWD { get; set; }
        public IDOBase VelStop { get; set; }
        public IDOBase Stop { get; set; }
        public IDOBase JogFRWD { get; set; }
        public IDOBase JogBKWD { get; set; }
        public IDOBase ToTargetPos { get; set; }
        #endregion DOBASE

        #region AOBASE
        public IAOBase ACCSet { get; set; }
        public IAOBase DECSet { get; set; }
        public IAOBase PosSet { get; set; }
        public IAOBase VelSet { get; set; }
        public IAOBase JogSet { get; set; }
        public IAOBase TargetSet { get; set; }
        #endregion AOBASE

        #endregion property
        public Motor()
        {
        }
        public Motor(string Name)
        {
            MotorName = Name;
            if(Init())
            {
                InitIOBase();
            }
        }
        public Motor(string unitname, string Name)
        {
            UnitName = unitname;
            MotorName = Name;
            if (Init())
            {
                InitIOBase();
            }
        }
        public Motor(string Name, IIOAccessor IOAccessor)
        {
            MotorName = Name;
            _ioAccessor = IOAccessor;
            Init();
        }
        public override void SetIOAccessor(IIOAccessor IOAccessor)
        {
            _ioAccessor = IOAccessor;
            ToTeachPos.SetIOAccessor(IOAccessor);
            AlarmReset.SetIOAccessor(IOAccessor);
            Disable.SetIOAccessor(IOAccessor);
            Enable.SetIOAccessor(IOAccessor);
            Home.SetIOAccessor(IOAccessor);
            VelSet.SetIOAccessor(IOAccessor);
            VelBKWD.SetIOAccessor(IOAccessor);
            VelFRWD.SetIOAccessor(IOAccessor);
            VelStop.SetIOAccessor(IOAccessor);
            Stop.SetIOAccessor(IOAccessor);
            JogBKWD.SetIOAccessor(IOAccessor);
            JogFRWD.SetIOAccessor(IOAccessor);
            ToTargetPos.SetIOAccessor(IOAccessor);
            //ACCSet.SetIOAccessor(IOAccessor);
            //DECSet.SetIOAccessor(IOAccessor);
            PosSet.SetIOAccessor(IOAccessor);
            VelSet.SetIOAccessor(IOAccessor);
            JogSet.SetIOAccessor(IOAccessor);
            TargetSet.SetIOAccessor(IOAccessor);
        }

        private bool Init()
        {
            try
            {
                string[] strings = MotorName.Split('_');
                string evMotorName = MotorName;
                if (strings.Length > 0 && !string.IsNullOrEmpty(UnitName) && strings[0] != UnitName.ToUpper())
                {
                    strings[0] = UnitName.ToUpper();
                    evMotorName = string.Join("_", strings);
                }


                DO_TO_TEACHPOS = NameConVertIO("DO_" + MotorName + "_TO_TEACHPOS");
                DO_ALARM_RESET = NameConVertIO("DO_" + MotorName + "_ALRM_RESET");
                DO_DISABLE = NameConVertIO("DO_" + MotorName + "_DISABLE");
                DO_ENABLE = NameConVertIO("DO_" + MotorName + "_ENABLE");
                DO_HOME_START = NameConVertIO("DO_" + MotorName + "_HOME_START");
                DO_VEL_BKWD = NameConVertIO("DO_" + MotorName + "_VEL_BKWD");
                DO_VEL_FRWD = NameConVertIO("DO_" + MotorName + "_VEL_FRWD");
                DO_STOP = NameConVertIO("DO_" + MotorName + "_STOP");
                DO_JOG_BKWD = NameConVertIO("DO_" + MotorName + "_JOG_BKWD");
                DO_JOG_FRWD = NameConVertIO("DO_" + MotorName + "_JOG_FRWD");
                DO_TO_TARGETPOS = NameConVertIO("DO_" + MotorName + "_TO_TARGETPOS");

                ToTeachStart = NameConVertEvent("EV_" + evMotorName + "_TO_TEACHPOS_START");
                AlarmResetStart = NameConVertEvent("EV_" + evMotorName + "_ALARM_RESET_START");
                DisableStart = NameConVertEvent("EV_" + evMotorName + "_DISABLE_START");
                EnableStart = NameConVertEvent("EV_" + evMotorName + "_ENABLE_START");
                HomeStart = NameConVertEvent("EV_" + evMotorName + "_HOME_START");
                VelBKWDStart = NameConVertEvent("EV_" + evMotorName + "_VEL_BKWD_START");
                VelFRWDStart = NameConVertEvent("EV_" + evMotorName + "_VEL_FRWD_START");
                VelStopStart = NameConVertEvent("EV_" + evMotorName + "_VEL_STOP_START");
                StopStart = NameConVertEvent("EV_" + evMotorName + "_STOP_START");
                JogBKWDStart = NameConVertEvent("EV_" + evMotorName + "_JOG_BKWD_START");
                JogFRWDStart = NameConVertEvent("EV_" + evMotorName + "_JOG_FRWD_START");
                ToTargetStart = NameConVertEvent("EV_" + evMotorName + "_TO_TARGETPOS_START");

                ToTeachCPLT = NameConVertEvent("EV_" + evMotorName + "_TO_TEACHPOS_CPLT");
                AlarmResetCPLT = NameConVertEvent("EV_" + evMotorName + "_ALARM_RESET_CPLT");
                DisableCPLT = NameConVertEvent("EV_" + evMotorName + "_DISABLE_CPLT");
                EnableCPLT = NameConVertEvent("EV_" + evMotorName + "_ENABLE_CPLT");
                HomeCPLT = NameConVertEvent("EV_" + evMotorName + "_HOME_CPLT");
                VelBKWDCPLT = NameConVertEvent("EV_" + evMotorName + "_VEL_BKWD_CPLT");
                VelFRWDCPLT = NameConVertEvent("EV_" + evMotorName + "_VEL_FRWD_CPLT");
                VelStopCPLT = NameConVertEvent("EV_" + evMotorName + "_VEL_STOP_CPLT");
                StopCPLT = NameConVertEvent("EV_" + evMotorName + "_STOP_CPLT");
                JogBKWDCPLT = NameConVertEvent("EV_" + evMotorName + "_JOG_BKWD_CPLT");
                JogFRWDCPLT = NameConVertEvent("EV_" + evMotorName + "_JOG_FRWD_CPLT");
                ToTargetCPLT = NameConVertEvent("EV_" + evMotorName + "_TO_TARGETPOS_CPLT");

                ToTeachTimeout = NameConVertEvent("AL_" + evMotorName + "_TO_TEACHPOS_FAIL");
                AlarmResetTimeout = NameConVertEvent("AL_" + evMotorName + "_ALARM_RESET_FAIL");
                DisableTimeout = NameConVertEvent("AL_" + evMotorName + "_DISABLE_FAIL");
                EnableTimeout = NameConVertEvent("AL_" + evMotorName + "_ENABLE_FAIL");
                HomeTimeout = NameConVertEvent("AL_" + evMotorName + "_HOME_FAIL");
                VelBKWDTimeout = NameConVertEvent("AL_" + evMotorName + "_VEL_BKWD_FAIL");
                VelFRWDTimeout = NameConVertEvent("AL_" + evMotorName + "_VEL_FRWD_FAIL");
                VelStopTimeout = NameConVertEvent("AL_" + evMotorName + "_VEL_STOP_FAIL");
                StopTimeout = NameConVertEvent("AL_" + evMotorName + "_STOP_FAIL");
                JogBKWDTimeout = NameConVertEvent("AL_" + evMotorName + "_JOG_BKWD_FAIL");
                JogFRWDTimeout = NameConVertEvent("AL_" + evMotorName + "_JOG_FRWD_FAIL");
                ToTargetTimeout = NameConVertEvent("AL_" + evMotorName + "_TO_TARGETPOS_FAIL");

                DI_AT_TEACHPOS = NameConVertIO("DI_" + MotorName + "_AT_TEACHPOS");
                DI_VEL_REACHED = NameConVertIO("DI_" + MotorName + "_VEL_REACHED");
                DI_DIR_BKWD = NameConVertIO("DI_" + MotorName + "_DIR_BKWD");
                DI_DIR_FRWD = NameConVertIO("DI_" + MotorName + "_DIR_FRWD");
                DI_DISABLED = NameConVertIO("DI_" + MotorName + "_DISABLED");
                DI_ENABLED = NameConVertIO("DI_" + MotorName + "_ENABLED");
                DI_HOME_CPLT = NameConVertIO("DI_" + MotorName + "_HOME_CPLT");
                DI_MOVING = NameConVertIO("DI_" + MotorName + "_MOVING");
                DI_NEGITIVE_LIMIT = NameConVertIO("DI_" + MotorName + "_NEGITIVE_LIMIT");
                DI_POSITIVE_LIMIT = NameConVertIO("DI_" + MotorName + "_POSITIVE_LIMIT");
                DI_AT_TARGET_CPLT = NameConVertIO("DI_" + MotorName + "_AT_TARGET_CPLT");
                //DI_Z2_HOME_SENSOR = NameConVertIO("DI_" + MotorName + "_HOME_SENSOR");


                AO_ACC_SET = NameConVertIO("AO_" + MotorName + "_ACC_SET");
                AO_DEC_SET = NameConVertIO("AO_" + MotorName + "_DEC_SET");
                AO_POS_SET = NameConVertIO("AO_" + MotorName + "_POS_SET");
                AO_VEL_SET = NameConVertIO("AO_" + MotorName + "_VEL_SET");
                AO_JOG_DISTANCE = NameConVertIO("AO_" + MotorName + "_JOG_DISTANCE");
                AO_TO_TARGET_SET = NameConVertIO("AO_" + MotorName + "_TO_TARGET_SET");

                //ACCSetStart = NameConVertEvent("EV_" + MotorName + "_ACC_SET_START");
                //DECSetStart = NameConVertEvent("EV_" + MotorName + "_DEC_SET_START");
                PosSetStart = NameConVertEvent("EV_" + evMotorName + "_POS_SET_START");
                VelSetStart = NameConVertEvent("EV_" + evMotorName + "_VEL_SET_START");
                JogDistanceSetStart = NameConVertEvent("EV_" + evMotorName + "_JOG_DISTANCE_SET_START");
                ToTargetSetStart = NameConVertEvent("EV_" + evMotorName + "_TO_TARGET_SET_START");

                //ACCSetCPLT = NameConVertEvent("EV_" + MotorName + "_ACC_SET_CPLT");
                //DECSetCPLT = NameConVertEvent("EV_" + MotorName + "_DEC_SET_CPLT");
                PosSetCPLT = NameConVertEvent("EV_" + evMotorName + "_POS_SET_CPLT");
                VelSetCPLT = NameConVertEvent("EV_" + evMotorName + "_VEL_SET_CPLT");
                JogDistanceSetCPLT = NameConVertEvent("EV_" + evMotorName + "_JOG_DISTANCE_SET_CPLT");
                ToTargetSetCPLT = NameConVertEvent("EV_" + evMotorName + "_TO_TARGET_SET_CPLT");

                //ACCSetTimeout = NameConVertEvent("AL_" + MotorName + "_ACC_SET_FAIL");
                //DECSetTimeout = NameConVertEvent("AL_" + MotorName + "_DEC_SET_FAIL");
                PosSetTimeout = NameConVertEvent("AL_" + evMotorName + "_POS_SET_FAIL");
                VelSetTimeout = NameConVertEvent("AL_" + evMotorName + "_VEL_SET_FAIL");
                JogDistanceSetTimeout = NameConVertEvent("AL_" + evMotorName + "_JOG_DISTANCE_SET_FAIL");
                ToTargetSetTimeout = NameConVertEvent("AL_" + evMotorName + "_TO_TARGET_SET_FAIL");

                AI_AXIS_ALRAM_CODE = NameConVertIO("AI_" + MotorName + "_AXIS_ALARM_CODE");
                AI_STEP_ERROR_CODE = NameConVertIO("AI_" + MotorName + "_STEP_ERROR_CODE");
                AI_AT_TARGET_POS = NameConVertIO("AI_" + MotorName + "_AT_TARGET_POS");
                AI_POS = NameConVertIO("AI_" + MotorName + "_POS");
                AI_TRQ = NameConVertIO("AI_" + MotorName + "_TRQ");
                AI_VEL = NameConVertIO("AI_" + MotorName + "_VEL");
                return true;
            }
            catch (Exception e)
            {
                Logger.Instance.WriteIOInfoLog($"ValveBase {MotorName} Init Error: {e}");
                return false;
            }
        }

        private void InitIOBase()
        {
            ToTargetPos = new MotorDAO(DO_TO_TARGETPOS,AO_TO_TARGET_SET,AI_AT_TARGET_POS, new List<IOID> { DI_AT_TARGET_CPLT }, new List<IOID> { DI_MOVING }, ToTargetStart, ToTargetCPLT, ToTargetTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            ToTeachPos = new MotorDAO(DO_TO_TEACHPOS,AO_POS_SET,AI_POS, new List<IOID> { DI_AT_TEACHPOS }, new List<IOID> { DI_MOVING }, ToTeachStart, ToTeachCPLT, ToTeachTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            AlarmReset = new AlarmCheckDO(DO_ALARM_RESET, new List<IOID> { AI_AXIS_ALRAM_CODE},  AlarmResetStart, AlarmResetCPLT, AlarmResetTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            Disable = new MotorTwoDO(DO_DISABLE, DO_ENABLE, new List<IOID> { DI_DISABLED }, new List<IOID> { DI_ENABLED }, DisableStart, DisableCPLT, DisableTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            Enable = new MotorTwoDO(DO_ENABLE, DO_DISABLE, new List<IOID> { DI_ENABLED }, new List<IOID> { DI_DISABLED }, EnableStart, EnableCPLT, EnableTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            Home = new MotorDO(DO_HOME_START, new List<IOID> { DI_HOME_CPLT }, new List<IOID> { DI_MOVING }  ,HomeStart, HomeCPLT, HomeTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            VelBKWD = new MotorVelDO(DO_VEL_BKWD, DO_VEL_FRWD, new List<IOID> { DI_VEL_REACHED, DI_DIR_BKWD ,DI_MOVING}, new List<IOID> { }, VelBKWDStart, VelBKWDCPLT, VelBKWDTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            VelFRWD = new MotorVelDO(DO_VEL_FRWD, DO_VEL_BKWD, new List<IOID> { DI_VEL_REACHED, DI_DIR_FRWD ,DI_MOVING}, new List<IOID> { }, VelFRWDStart, VelFRWDCPLT, VelFRWDTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            //TODO 需添加一个VelStop的
            VelStop = new MotorVelStopDO(DO_VEL_FRWD, DO_VEL_BKWD, new List<IOID> { }, new List<IOID> { DI_MOVING }, VelStopStart, VelStopCPLT, VelStopTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            Stop = new MotorDO(DO_STOP, new List<IOID> { }, new List<IOID> { DI_MOVING }, StopStart, StopCPLT, StopTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            JogFRWD = new MotorTwoDO(DO_JOG_FRWD, DO_JOG_BKWD, new List<IOID> { },new List<IOID> { DI_MOVING }, JogFRWDStart, JogFRWDCPLT, JogFRWDTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            JogBKWD = new MotorTwoDO(DO_JOG_BKWD, DO_JOG_FRWD, new List<IOID> { }, new List<IOID> { DI_MOVING }, JogBKWDStart, JogBKWDCPLT, JogBKWDTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);

            //ACCSet = new AOBase(AO_ACC_SET, ACCSetStart, ACCSetCPLT, ACCSetTimeout, ECID.EC_ST_MOTOR_TO_TARGET_TIMEOUT);
            //DECSet = new AOBase(AO_DEC_SET, DECSetStart, DECSetCPLT, DECSetTimeout, ECID.EC_ST_MOTOR_TO_TARGET_TIMEOUT);
            PosSet = new AOBase(AO_POS_SET, PosSetStart, PosSetCPLT, PosSetTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            VelSet= new AOBase(AO_VEL_SET, VelSetStart, VelSetCPLT, VelSetTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            JogSet = new AOBase(AO_JOG_DISTANCE, JogDistanceSetStart, JogDistanceSetCPLT, JogDistanceSetTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
            TargetSet = new AOBase(AO_TO_TARGET_SET, ToTargetSetStart, ToTargetSetCPLT, ToTargetSetTimeout, ECID.EC_MOTOR_OPERATION_TIMEOUT);
        }
    }
}
