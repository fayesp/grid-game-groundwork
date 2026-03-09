using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace Assets.Scripts
{
    public class MyLogger
    {
        #region property
        public ConcurrentQueue<string> InfoQueue { get; set; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> ErrorQueue { get; set; } = new ConcurrentQueue<string>();
        ManualResetEvent threadMarkup = new ManualResetEvent(false);
        #endregion property
        #region construtor
        public MyLogger() { WriteAsync(); }
        #endregion construtor
        #region instance
        private static MyLogger instance;
        public static MyLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MyLogger();
                }
                return instance;
            }
        }
        #endregion instance
        #region Path
        public string CurrentDirectory = "";
        private static string _home = "";
        public static string HomePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_home))
                {
                    string home = "MAGNET_HOME_PATH";
                    _home = Environment.GetEnvironmentVariable(home);
                }
                return _home;
            }
        }
        public void RefreshLogPath()
        {
            CurrentDirectory = Path.Combine(HomePath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(CurrentDirectory))
            {
                Directory.CreateDirectory(CurrentDirectory);
            }
        }
        string InfoFileName = "info.log";
        string ErrorFileName = "error.log";
        #endregion Path
        #region StreamWrite
        public StreamWriter SW;
        public void GetWriteString(string FileName)
        {
            RefreshLogPath();
            string filePath = $"{CurrentDirectory}\\{FileName}";
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close(); // 确保文件存在
            }
            if (SW != null)
            {
                SW.Close();
            }
            SW = new StreamWriter(filePath, true);
        }
        public void WriteInfo(string msg)
        {
            InfoQueue.Enqueue(msg);
            threadMarkup.Set();
        }
        public void WriteError(string msg)
        {
            ErrorQueue.Enqueue(msg);
            threadMarkup.Set();
        }
        public void WriteAsync()
        {
            var task = Task.Factory.StartNew
                (() =>
                {
                    string msg;
                    threadMarkup.WaitOne();
                    while (InfoQueue.Count > 0 || ErrorQueue.Count > 0)
                    {
                        GetWriteString(InfoFileName);
                        while (InfoQueue.Count > 0)
                        {
                            InfoQueue.TryDequeue(out msg);
                            SW.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {msg}");
                            SW.Flush();
                        }
                        GetWriteString(ErrorFileName);
                        while (ErrorQueue.Count > 0)
                        {
                            ErrorQueue.TryDequeue(out msg);
                            SW.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {msg}");
                            SW.Flush();
                        }
                        threadMarkup.Reset();
                        Thread.Sleep(2);
                    }
                }, TaskCreationOptions.LongRunning);
            task.ContinueWith(t =>
            {
                foreach (var ex in t.Exception.Flatten().InnerExceptions)
                {
                    WriteOSEvent(string.Format("[LogHandler:WriteAsync]:Write log into file fail,Detail:{0},StackTrace:{1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        void WriteOSEvent(string msg, EventLogEntryType type)
        {
            System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = "Log Service";
            eventLog.WriteEntry(msg, type);
        }
        #endregion StreamWrite
    }
}