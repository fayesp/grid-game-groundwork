using Common.RTSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.ComponentModel;
using System.Web;
using Common.Log;
using IECommonEntiry.Entity;

namespace Common.RTSystem.Factory
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"Can Not Found Key: {key} In IOConfig");
            return defaultValue;
        }
    }
    public abstract class FactoryBase
    {
        protected string IOXml = SystemEnvironment.DataFolder + "\\Config\\IOConfig.xml";

        protected FactoryBase()
        {
            Init();
        }
        public abstract void Init();
    }
    public class MotorFactory : FactoryBase
    {

        public Dictionary<string, IMotorBase> Motors = new Dictionary<string, IMotorBase>();
        private static readonly MotorFactory _instance = new MotorFactory();

        public static MotorFactory Instance
        {
            get { return _instance; }
        }

        public override void Init()
        {
            try
            {
                if (!File.Exists(IOXml))
                {
                    throw new Exception($"No found {IOXml}");
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(IOXml);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("/configuration/ST/Motors/Motor");

                foreach (XmlNode node in nodes)
                {
                    Type algTypeGen = Assembly.Load("Extraction").GetType(node.Attributes["GetType"].Value);
                    Motor motor = (Motor)Activator.CreateInstance(algTypeGen, new object[] { node.Attributes["IOName"].Value });
                    XmlNodeList motors = node.SelectNodes("/motors/motor");
                    if (!Motors.ContainsKey(node.Attributes["IOName"].Value))
                    {
                        Motors.Add(node.Attributes["IOName"].Value, motor);
                    }
                    else
                    {
                        Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {node.Attributes["IOName"].Value} have same name");
                    }

                }

            }
            catch (Exception e)
            {
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"{e}");
            }

        }
    }
    public class ValveFactory : FactoryBase
    {
        public Dictionary<string, IValveBase> Valves = new Dictionary<string, IValveBase>();
        private static readonly ValveFactory _instance = new ValveFactory();

        public static ValveFactory Instance
        {
            get { return _instance; }
        }

        public override void Init()
        {
            try
            {
                if (!File.Exists(IOXml))
                {
                    throw new Exception($"No found {IOXml}");
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(IOXml);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("/configuration/ST/Valves/Valve");

                foreach (XmlNode node in nodes)
                {
                    Type algTypeGen = Assembly.Load("Extraction").GetType(node.Attributes["GetType"].Value);
                    Valve Valve = (Valve)Activator.CreateInstance(algTypeGen, new object[] { node.Attributes["ValveName"].Value });
                    if (!Valves.ContainsKey(node.Attributes["ValveName"].Value))
                    {
                        Valves.Add(node.Attributes["ValveName"].Value, Valve);
                    }
                    else
                    {
                        Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {node.Attributes["ValveName"].Value} have same name");
                    }

                }

            }
            catch (Exception e)
            {
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"{e}");
            }


        }
    }
    public class CylinderFactory : FactoryBase
    {
        public Dictionary<string, ICylinderBase> Cylinders = new Dictionary<string, ICylinderBase>();
        private static readonly CylinderFactory _instance = new CylinderFactory();

        public static CylinderFactory Instance
        {
            get { return _instance; }
        }

        public override void Init()
        {
            try
            {
                if (!File.Exists(IOXml))
                {
                    throw new Exception($"No found {IOXml}");
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(IOXml);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("/configuration/ST/Cylinders/Cylinder");

                foreach (XmlNode node in nodes)
                {
                    Type algTypeGen = Assembly.Load("Extraction").GetType(node.Attributes["GetType"].Value);
                    List<string> DI_OnList = new List<string>();
                    List<string> DI_OffList = new List<string>();
                    XmlNode DI_OnNode = node.SelectSingleNode("DI_OnList");
                    XmlNode DI_OffNode = node.SelectSingleNode("DI_OffList");
                    foreach (XmlNode diOn in DI_OnNode.ChildNodes)
                    {
                        DI_OnList.Add(diOn.InnerText);
                    }
                    foreach (XmlNode diOff in DI_OffNode.ChildNodes)
                    {
                        DI_OffList.Add(diOff.InnerText);
                    }
                    Cylinder Cylinder = (Cylinder)Activator.CreateInstance(algTypeGen, new object[]
                    {
                        node.Attributes["CylinderName"].Value,
                        node.Attributes["DO_On"].Value,
                        node.Attributes["DO_Off"].Value,
                        DI_OnList,
                        DI_OffList,
                    });
                    if (!Cylinders.ContainsKey(node.Attributes["CylinderName"].Value))
                    {
                        Cylinders.Add(node.Attributes["CylinderName"].Value, Cylinder);
                    }
                    else
                    {
                        Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {node.Attributes["CylinderName"].Value} have same name");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"{e}");
            }
        }
    }



    public class DOFactory : FactoryBase
    {
        public Dictionary<string, IDOBase> DOs = new Dictionary<string, IDOBase>();
        private static readonly DOFactory _instance = new DOFactory();
        public static DOFactory Instance
        {
            get { return _instance; }
        }
        public override void Init()
        {
            try
            {
                if (!File.Exists(IOXml))
                {
                    throw new Exception($"No found {IOXml}");
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(IOXml);
                XmlNodeList nodes = doc.DocumentElement.SelectNodes("/configuration/DOs/DO");
                foreach (XmlNode node in nodes)
                {
                    Type algTypeGen = Assembly.Load("Extraction").GetType(node.Attributes["GetType"].Value);
                    List<string> DI_OnList = new List<string>();
                    List<string> DI_OffList = new List<string>();
                    XmlNode DI_OnNode = node.FirstChild;
                    XmlNode DI_OffNode = node.LastChild;
                    if (DI_OnNode.Name == "DI_OnList")
                    {
                        foreach (XmlNode diOn in DI_OnNode.ChildNodes)
                        {
                            DI_OnList.Add(diOn.InnerText);
                        }
                    }
                    if (DI_OffNode.Name == "DI_OffList")
                    {
                        foreach (XmlNode diOff in DI_OffNode.ChildNodes)
                        {
                            DI_OffList.Add(diOff.InnerText);
                        }
                    }
                    DOBase DO = (DOBase)Activator.CreateInstance(algTypeGen, new object[] 
                    { 
                        node.Attributes["DO_On"].Value,
                        node.Attributes["DO_Off"].Value,
                        DI_OnList,
                        DI_OffList,
                        node.Attributes["StartEvent"].Value,
                        node.Attributes["EndEvent"].Value,
                        node.Attributes["AlarmEvent"].Value,
                        node.Attributes["TimeoutEC"].Value
                    });
                    if (!DOs.ContainsKey(node.Attributes["Name"].Value))
                    {
                        DOs.Add(node.Attributes["Name"].Value, DO);
                    }
                    else
                    {
                        Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {node.Attributes["Name"].Value} have same name");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Instance.WriteIOLog( enLogType.ERROR, $"{e}");
            }
        }
    }

    public class  AOFactory : FactoryBase    
    {
        public Dictionary<string, IAOBase> AOs = new Dictionary<string, IAOBase>();
        private static readonly AOFactory _instance = new AOFactory();
        public static AOFactory Instance
        {
            get { return _instance; }
        }
        public override void Init()
        {
            try
            {
                if (!File.Exists(IOXml))
                {
                    throw new Exception($"No found {IOXml}");
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(IOXml);
                XmlNodeList nodes = doc.DocumentElement.SelectNodes("/configuration/AOs/AO");
                foreach (XmlNode node in nodes)
                {
                    Type algTypeGen = Assembly.Load("Extraction").GetType(node.Attributes["GetType"].Value);
                    AOBase AO = (AOBase)Activator.CreateInstance(algTypeGen, new object[] 
                    { 
                        node.Attributes["AOName"].Value,
                        node.Attributes["StartEvent"].Value,
                        node.Attributes["EndEvent"].Value,
                        node.Attributes["AlarmEvent"].Value,
                        node.Attributes["TimeoutEC"].Value
                    });
                    if (!AOs.ContainsKey(node.Attributes["Name"].Value))
                    {
                        AOs.Add(node.Attributes["Name"].Value, AO);
                    }
                    else
                    {
                        Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {node.Attributes["Name"].Value} have same name");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"{e}");
            }
        }
    }

    public class UnitFactory : FactoryBase
    {
        public Dictionary<string, IUnitBase> Units = new Dictionary<string, IUnitBase>();
        private static readonly UnitFactory _instance = new UnitFactory();
        public static UnitFactory Instance
        {
            get { return _instance; }
        }
        public override void Init()
        {
            try
            {
                if (!File.Exists(IOXml))
                {
                    throw new Exception($"No found {IOXml}");
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(IOXml);
                XmlNodeList UnitNodes = doc.DocumentElement.SelectNodes("/configuration/Units/Unit");
                foreach (XmlNode UnitNode in UnitNodes)
                {
                    Unit Unit = new Unit(UnitNode.Attributes["UnitName"].Value);
                    if (!Units.ContainsKey(UnitNode.Attributes["UnitName"].Value))
                    {
                        Units.Add(UnitNode.Attributes["UnitName"].Value, Unit);
                        XmlNodeList ValveNodes = UnitNode.SelectNodes("Valves/Valve");
                        foreach (XmlNode ValveNode in ValveNodes)
                        {
                            Type algTypeGen = Assembly.Load("Extraction").GetType(ValveNode.Attributes["GetType"].Value);
                            Valve Valve;
                            if (ValveNode.Attributes["DO_On"] != null && ValveNode.Attributes["DO_Off"] != null)
                            {
                                Valve = (Valve)Activator.CreateInstance(algTypeGen, new object[]
                                {
                                   UnitNode.Attributes["UnitName"].Value,
                                   ValveNode.Attributes["ValveName"].Value,
                                   ValveNode.Attributes["DO_On"].Value,
                                   ValveNode.Attributes["DO_Off"].Value,
                                   ValveNode.Attributes["DI_On"].Value,
                                   ValveNode.Attributes["DI_Off"].Value,
                                });
                            }
                            else
                            {
                                Valve = (Valve)Activator.CreateInstance(algTypeGen, new object[]
                                {
                                   UnitNode.Attributes["UnitName"].Value,
                                   ValveNode.Attributes["ValveName"].Value,
                                });
                            }
                            if (!Unit.Valves.ContainsKey(ValveNode.Attributes["ValveName"].Value))
                            {
                                Unit.Valves.Add(ValveNode.Attributes["ValveName"].Value, Valve);
                            }
                            else
                            {
                                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {ValveNode.Attributes["ValveName"].Value} have same name");
                            }
                        }
                        XmlNodeList CylinderNodes = UnitNode.SelectNodes("Cylinders/Cylinder");
                        foreach (XmlNode CylinderNode in CylinderNodes)
                        {
                            Type algTypeGen = Assembly.Load("Extraction").GetType(CylinderNode.Attributes["GetType"].Value);
                            Cylinder Cylinder;
                            if (CylinderNode.Attributes["DI_On"] != null && CylinderNode.Attributes["DI_Off"] != null)
                            {
                                Cylinder = (Cylinder)Activator.CreateInstance(algTypeGen, new object[]
                                {
                                    UnitNode.Attributes["UnitName"].Value,
                                    CylinderNode.Attributes["CylinderName"].Value,
                                    CylinderNode.Attributes["DO_On"].Value,
                                    CylinderNode.Attributes["DO_Off"].Value,
                                    CylinderNode.Attributes["DI_On"].Value.Split(',').ToList(),
                                    CylinderNode.Attributes["DI_Off"].Value.Split(',').ToList(),
                                });
                            }
                            else
                            {
                                Cylinder = (Cylinder)Activator.CreateInstance(algTypeGen, new object[]
                                {
                                    UnitNode.Attributes["UnitName"].Value,
                                    CylinderNode.Attributes["CylinderName"].Value,
                                    CylinderNode.Attributes["DO_On"].Value,
                                    CylinderNode.Attributes["DO_Off"].Value,                         
                                });
                            }
                            if (!Unit.Cylinders.ContainsKey(CylinderNode.Attributes["CylinderName"].Value))
                            {
                                Unit.Cylinders.Add(CylinderNode.Attributes["CylinderName"].Value, Cylinder);
                            }
                            else
                            {
                                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {CylinderNode.Attributes["CylinderName"].Value} have same name");
                            }
                        }
                        XmlNodeList MotorNodes = UnitNode.SelectNodes("Motors/Motor");
                        foreach (XmlNode MotorNode in MotorNodes)
                        {
                            Type algTypeGen = Assembly.Load("Extraction").GetType(MotorNode.Attributes["GetType"].Value);
                            Motor motor = (Motor)Activator.CreateInstance(algTypeGen, new object[] { UnitNode.Attributes["UnitName"].Value, MotorNode.Attributes["MotorName"].Value });
                            if (!Unit.Motors.ContainsKey(MotorNode.Attributes["MotorName"].Value))
                            {
                                Unit.Motors.Add(MotorNode.Attributes["MotorName"].Value, motor);
                            }
                            else
                            {
                                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {MotorNode.Attributes["MotorName"].Value} have same name");
                            }
                        }
                        //Units.Add(UnitNode.Attributes["UnitName"].Value, Unit);
                        XmlNodeList DONodes = UnitNode.SelectNodes("DOS/SingleDO");
                        foreach (XmlNode DONode in DONodes)
                        {
                            Type algTypeGen = Assembly.Load("Extraction").GetType(DONode.Attributes["GetType"].Value);
                            string DO = DONode.Attributes["DO"].Value;
                            string DI = DONode.Attributes["DI"].Value;

                            DOBase SingleDO = (DOBase)Activator.CreateInstance(algTypeGen, new object[]
                            {
                                DONode.Attributes["DO"].Value,
                                DONode.Attributes["DI"].Value,
                            });
                            if (!Unit.DOs.ContainsKey(DONode.Attributes["SingleDOName"].Value))
                            {
                                Unit.DOs.Add(DONode.Attributes["SingleDOName"].Value, SingleDO);
                            }
                            else
                            {
                                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {DONode.Attributes["CylinderName"].Value} have same name");
                            }
                        }
                    }
                    else
                    {
                        Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $" Key: {UnitNode.Attributes["UnitName"].Value} have same name");
                    }
                }
                
            }
            catch (Exception e)
            {
                Log.Logger.Instance.WriteIOLog(enLogType.ERROR, $"{e}");
            }
        }
    }

    //使用UnitFactory生成的配对的IO,生成模拟器配置文件
    public class SimulatorConfig
    {
        private static SimulatorConfig _simulatorConfig;
        public static SimulatorConfig Instance
        {
            get
            {
                if (_simulatorConfig == null)
                {
                    _simulatorConfig = new SimulatorConfig();
                }
                return _simulatorConfig;
            }
        }
        public string SimulatorPath = SystemEnvironment.DataFolder + "\\Simulator";
        protected string IOXMLPath = SystemEnvironment.DataFolder + "\\Simulator\\IOXML";
        protected string TempPath = SystemEnvironment.DataFolder + "\\Simulator\\Temp";
        protected string TemplatePath = SystemEnvironment.DataFolder + "\\Simulator\\Template";
        protected string ScenarioPath => TempPath + "\\Scenario.txt";
        protected string ValvePath => TempPath + "\\Valves.txt";
        protected string CylinderPath => TempPath + "\\Cylinders.txt";
        protected string MotorPath => TempPath + "\\Motors.txt";
        protected string SingleDOPath => TempPath + "\\SingleDO.txt";

        Dictionary<string, IUnitBase> Units { get => UnitFactory.Instance.Units; }
        public SimulatorConfig()
        {
            
        }
        public void SimulatorConfigGenerate()
        {
            try
            {
                if (!Directory.Exists(SimulatorPath))
                {
                    Directory.CreateDirectory(SimulatorPath);
                }
                if (!Directory.Exists(IOXMLPath))
                {
                    Directory.CreateDirectory(IOXMLPath);
                }
                if (!Directory.Exists(TempPath))
                {
                    Directory.CreateDirectory(TempPath);
                }
                if (!Directory.Exists(TemplatePath))
                {
                    Directory.CreateDirectory(TemplatePath);
                }
                string ValveTemplate = ReadFromFile(TemplatePath + "\\ValveTemplate.txt");
                string CylinderTemplate = ReadFromFile(TemplatePath + "\\CylinderTemplate.txt");
                string MotorTemplate = ReadFromFile(TemplatePath + "\\MotorTemplate.txt");
                string ScenarioTemplate = ReadFromFile(TemplatePath + "\\ScenarioTemplate.txt");
                string SingleDOTemplate = ReadFromFile(TemplatePath + "\\SingleDOTemplate.txt");
                string OriginTemplate = ReadFromFile(TemplatePath + "\\OriginTemplate.txt");
                foreach (var unit in Units)
                {
                    WriteToFile(ScenarioPath, string.Empty, false);
                    WriteToFile(ValvePath, string.Empty, false);
                    WriteToFile(CylinderPath, string.Empty, false);
                    WriteToFile(MotorPath, string.Empty, false);
                    WriteToFile(SingleDOPath, string.Empty, false);
                    foreach (var valve in unit.Value.Valves.Values)
                    {
                        string valveContent = string.Format(ValveTemplate, valve.DO_VALVE_ON, valve.DI_VALVE_ON_RSP,
                            valve.DO_VALVE_OFF, valve.DI_VALVE_OFF_RSP);
                        WriteToFile(ValvePath, valveContent, true);
                    }
                    foreach (var cylinder in unit.Value.Cylinders.Values)
                    {
                        string cylinderContent = string.Format(CylinderTemplate, cylinder.DO_CYLINDER_ON, cylinder.DI_CYLINDER_ON[0], cylinder.DO_CYLINDER_OFF, cylinder.DI_CYLINDER_OFF[0], cylinder.DI_CYLINDER_ON[1], cylinder.DI_CYLINDER_OFF[1]);
                        WriteToFile(CylinderPath, cylinderContent, true);
                    }
                    foreach (var motor in unit.Value.Motors.Values)
                    {
                        string motorContent = string.Format(MotorTemplate, motor.MotorName, motor.DO_ALARM_RESET, motor.DO_DISABLE, motor.DO_ENABLE, motor.DO_HOME_START, motor.DO_JOG_BKWD, motor.DO_JOG_FRWD, motor.DO_STOP, motor.DO_TO_TARGETPOS, motor.DO_TO_TEACHPOS, motor.DO_VEL_BKWD, motor.DO_VEL_FRWD, motor.DI_AT_TARGET_CPLT, motor.DI_AT_TEACHPOS, motor.DI_DIR_BKWD, motor.DI_DIR_FRWD, motor.DI_DISABLED, motor.DI_ENABLED, motor.DI_HOME_CPLT, motor.DI_MOVING, motor.DI_VEL_REACHED, motor.AO_ACC_SET, motor.AO_DEC_SET, motor.AO_JOG_DISTANCE, motor.AO_POS_SET, motor.AO_TO_TARGET_SET, motor.AO_VEL_SET, motor.AI_AT_TARGET_POS, motor.AI_AXIS_ALRAM_CODE, motor.AI_POS, motor.AI_STEP_ERROR_CODE, motor.AI_VEL, unit.Value.UnitName);
                        WriteToFile(MotorPath, motorContent, true);
                    }
                    foreach (var motor in unit.Value.Motors.Values)
                    {
                        string scenarioContent = string.Format(ScenarioTemplate, unit.Value.UnitName, motor.MotorName, motor.DO_VEL_FRWD, motor.DO_VEL_BKWD, motor.DI_DIR_FRWD, motor.DI_DIR_BKWD, motor.DI_ENABLED, motor.AO_VEL_SET, motor.AO_ACC_SET, motor.AO_DEC_SET, motor.AI_VEL, motor.AI_POS, motor.DI_DISABLED, motor.DI_MOVING);
                        WriteToFile(ScenarioPath, scenarioContent, true);
                    }
                    foreach (var DO in unit.Value.DOs.Values)
                    {
                        string singleDOContent = string.Format(SingleDOTemplate, DO.do_True, DO.DI_TRUE[0]);
                        WriteToFile(SingleDOPath, singleDOContent, true);
                    }
                    string ScenarioTemp = ReadFromFile(ScenarioPath);
                    string ValveTemp = ReadFromFile(ValvePath);
                    string CylinderTemp = ReadFromFile(CylinderPath);
                    string MotorTemp = ReadFromFile(MotorPath);
                    string SingleDOTemp = ReadFromFile(SingleDOPath);
                    string FinalContent = string.Format(OriginTemplate, unit.Value.UnitName, ScenarioTemp, ValveTemp, CylinderTemp, MotorTemp, SingleDOTemp);
                    string FilePath = IOXMLPath + $"\\{unit.Value.UnitName}IO.xml";
                    WriteToFile(FilePath, FinalContent, false);
                }
            }
            catch (Exception ex) 
            {
                Log.Logger.Instance.WriteRTDebugErrorLog(UNIT_ID.UNIT_ID_SYS,$"SimulatorConfigGenerate Debug error:{ex}");
            }
        }
        /// <summary>
        /// Writes the specified content to a file at the given path, with an option to append to the file.
        /// </summary>
        /// <remarks>If the file does not exist, it will be created. If an error occurs during the write
        /// operation,  an error message will be logged to the console.</remarks>
        /// <param name="filePath">The path of the file to write to. Must be a valid file path.</param>
        /// <param name="content">The content to write to the file.</param>
        /// <param name="append">A value indicating whether to append the content to the file.  <see langword="true"/> to append to the file
        /// if it exists; <see langword="false"/> to overwrite the file.</param>
        static void WriteToFile(string filePath, string content, bool append)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Instance.WriteIOLog(enLogType.ERROR, $"{filePath} not exist");
                }
                using (StreamWriter writer = new StreamWriter(filePath, append))
                {
                    writer.Write(content);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteIOLog(enLogType.ERROR,"An error occurred while writing to the file: " + ex.Message);
            }
        }
        /// <summary>
        /// Reads the contents of a file and returns it as a string.
        /// </summary>
        /// <remarks>This method attempts to read the entire contents of the file specified by <paramref
        /// name="filePath"/>.  If an error occurs during the read operation, the method logs the error to the console
        /// and returns an empty string.</remarks>
        /// <param name="filePath">The full path to the file to be read. This cannot be null or empty.</param>
        /// <returns>A string containing the contents of the file. Returns an empty string if an error occurs while reading the
        /// file.</returns>
        static string ReadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Instance.WriteIOLog(enLogType.ERROR, $"{filePath} not exist");
                }
                using (StreamReader reader = new StreamReader(filePath))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteIOLog(enLogType.ERROR, "An error occurred while read from file: " + ex.Message);
                return string.Empty;
            }
        }

    }

}
