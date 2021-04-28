using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace OverWatch
{
  class Program
  {
    [STAThread]
    static void Main(string[] args)
    {
      try
      {
        using (VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication("SysAdmin", "SysAdmin"))
        {
          Execute(app);
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e.ToString());
      }
          


    }
        static void InitializeAndStartMainWindow(EsapiWorker es)
        {
            Window window = new Window();
            UserControl1 mainWindow = new UserControl1(es);
            window.Title = "OverWatch 0.1";
            window.Content = mainWindow;
            window.Width = 300;
            window.Height = 300;
            window.ShowDialog();

        }

        static void Execute(VMS.TPS.Common.Model.API.Application app)
    {
            // TODO: add here your code
            var esapiWorker = new EsapiWorker(app);

            // This new queue of tasks will prevent the script
            // for exiting until the new window is closed
            DispatcherFrame frame = new DispatcherFrame();

            RunOnNewStaThread(() =>
            {
                // This method won't return until the window is closed
                InitializeAndStartMainWindow(esapiWorker);

                // End the queue so that the script can exit
                frame.Continue = false;
            });

            // Start the new queue, waiting until the window is closed
            Dispatcher.PushFrame(frame);

        }
        static void RunOnNewStaThread(Action a)
        {
            Thread thread = new Thread(() => a());
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }
    }

    public class EsapiWorker
    {
        private readonly VMS.TPS.Common.Model.API.Application _app;
        private readonly Dispatcher _dispatcher;

        public EsapiWorker(VMS.TPS.Common.Model.API.Application app)
        {
            _app = app;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Run(Action<VMS.TPS.Common.Model.API.Application> a)
        {
            _dispatcher.BeginInvoke(a, _app);
        }
    }
}
//select distinct nsa.WorkFlowActiveFlag, nsa.ObjectStatus, p.PatientId, vva.Expression1, nsa.NonScheduledActivityCode
//from vv_ActivityLng vva, NonScheduledActivity nsa, ActivityInstance ai, Activity a, ActivityCategory ac, Patient p
//where nsa.ActivityInstanceSer = ai.ActivityInstanceSer
//and nsa.PatientSer = p.PatientSer
//and nsa.WorkFlowActiveFlag=1
//and nsa.ActivityInstanceSer = ai.ActivityInstanceSer
//and ai.ActivitySer = a.ActivitySer
//and ac.ActivityCategorySer = a.ActivityCategorySer
//and nsa.NonScheduledActivityCode = 'Open'
//and vva.Expression1 = 'Physics Plan Check'
//and vva.LookupValue = a.ActivityCode
//and ai.ObjectStatus = 'Active'
//and ac.DepartmentSer = 10002.00