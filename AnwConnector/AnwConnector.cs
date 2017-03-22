using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using AnwConnector.Model;
using AnwConnector.Util;
using System.Reflection;

namespace AnwConnector
{
    public partial class AnwConnector : ServiceBase
    {
        internal readonly List<ServiceJob> ServiceJobs = new List<ServiceJob>();
        //用哈希表存放任务项
        public AnwConnector()
        {
            InitializeComponent();
            var thisExe = Assembly.GetExecutingAssembly();
            foreach (var type in thisExe.GetTypes())
            {
                if (type.BaseType == typeof(ServiceJob))
                {
                    var instance = (ServiceJob)Activator.CreateInstance(type);
                    ServiceJobs.Add(instance);
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                foreach (var serviceJob in ServiceJobs)
                {
                    LogHelper.WriteLog("正在启动任务:" + serviceJob.GetType().Name);
                    serviceJob.CancelTokenSource = new CancellationTokenSource();
                    serviceJob.StartJob();
                    LogHelper.WriteLog(serviceJob.GetType().Name + "成功启动!");
                }
            }
            catch (Exception exception)
            {
                LogHelper.WriteLog(exception.Message, exception);
            }
        }

        protected override void OnStop()
        {
            try
            {
                foreach (var serviceJob in ServiceJobs)
                {
                    LogHelper.WriteLog("正在停止任务:" + serviceJob.GetType().Name);
                    serviceJob.StopJob();
                    LogHelper.WriteLog(serviceJob.GetType().Name + "成功停止!");
                }
            }
            catch (Exception exception)
            {
                LogHelper.WriteLog(exception.Message, exception);
            }
        }

        protected override void OnShutdown()
        {
            SapDiHelper.Discount();
        }
    }
}