using Common;
using IECommonEntiry.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common.RTSystem
{

    public class IOBase
    {
        public IOID NameConVertIO(string IOName)
        {

            if (Enum.TryParse(IOName, out IOID IO))
            {
                // 成功转换
            }
            else
            {

                // 转换失败
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"IOName:{IOName} Can Not Convert To IOID Type");
            }
            return IO;
        }
        public EventID NameConVertEvent(string eventName)
        {
            //转换失败返回值为0的枚举,设置默认的EventID,EventID.unknown
            if (Enum.TryParse(eventName, out EventID eventID))
            {
                // 成功转换
            }
            else
            {
                // 转换失败
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"EventName:{eventName} Can Not Convert To EventID Type");
            }
            return eventID;
        }
        public ECID NameConVertEC(string ecName)
        {
            if (Enum.TryParse(ecName, out ECID ecID))
            {
                // 成功转换
            }
            else
            {
                // 转换失败
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"ECName:{ecName} Can Not Convert To ECID Type");
            }
            return ecID;
        }
        protected IIOAccessor _ioAccessor = null;
        public IIOAccessor IOAccessor { get { return _ioAccessor; } }
        public virtual void SetIOAccessor(IIOAccessor IOAccessor)
        {
            _ioAccessor = IOAccessor;
        }
        #region Read/WriteIO
        protected bool ReadDI(IOID io)
        {
            try
            {
                if (IOAccessor == null) return false;
                return IOAccessor.ReadDI(io);
            }
            catch
            {
                return false;
            }
        }
        protected int ReadIAI(IOID io)
        {
            try
            {
                if (IOAccessor == null) return 0;
                return IOAccessor.ReadIAI(io);
            }
            catch
            {
                return 0;
            }
        }
        protected float ReadFAI(IOID io)
        {
            try
            {
                if (IOAccessor == null) return 0;
                return IOAccessor.ReadFAI(io);
            }
            catch
            {
                return 0;
            }
        }
        protected double ReadDAI(IOID io)
        {
            try
            {
                if (IOAccessor == null) return 0;
                return IOAccessor.ReadDAI(io);
            }
            catch
            {
                return 0;
            }
        }
        protected bool WriteIO(IOID io, bool value)
        {
            if (IOAccessor == null)
            {
                return false;
            }

            return IOAccessor.WriteIO(io, value);
        }

        protected bool WriteIO(IOID io, int value)
        {
            if (IOAccessor == null)
            {
                return false;
            }

            return IOAccessor.WriteIO(io, value);
        }

        protected bool WriteIO(IOID io, float value)
        {
            if (IOAccessor == null)
            {
                return false;
            }

            return IOAccessor.WriteIO(io, value);
        }

        protected bool WriteDO(IOID trueDO, IOID falseDO)
        {
            bool flag = WriteIO(trueDO, value: true);
            bool flag2 = WriteIO(falseDO, value: false);
            return flag && flag2;
        }


        #endregion
    }
}
