using Common.RTSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTSystem
{
    public interface IUnitBase
    {
        string UnitName { get; set; }
        Dictionary<string,IValveBase> Valves { get; set; }
        Dictionary<string, ICylinderBase> Cylinders { get; set; }
        Dictionary<string,IMotorBase> Motors { get; set; }
        Dictionary<string,DOBase> DOs { get; set; }
    }
    public class Unit : IUnitBase
    {
        public string UnitName { get; set; }
        public Unit(string unitName)
        {
            UnitName = unitName;
            Valves = new Dictionary<string, IValveBase>();
            Cylinders = new Dictionary<string, ICylinderBase>();
            Motors = new Dictionary<string, IMotorBase>();
            DOs = new Dictionary<string, DOBase>();
        }

        public Dictionary<string, IValveBase> Valves { get; set; }
        public Dictionary<string, ICylinderBase> Cylinders { get; set; }
        public Dictionary<string, IMotorBase> Motors { get; set; }
        public Dictionary<string, DOBase> DOs { get; set; }
    }
}
