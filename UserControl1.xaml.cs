using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Data.SqlClient;
using System.Data;
using System.Net.Mail;
using System.IO;

namespace OverWatch
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        //private VMS.TPS.Common.Model.API.Application _appl;
        Thread t1;
        public TextBox tbg_loop;
        EsapiWorker _es;
        public UserControl1(EsapiWorker es)
        {
            //_appl = Application;
            _es = es;
            InitializeComponent();
            tb_loop.Text = $"Stopped.";
        }

        public void daemon()
        {
            int loops = 0;
            List<string> exclusion = new List<string>();



            string owner;
            //mail.Body = "this is my test email body";



            //client.Send(mail);

            this.Dispatcher.Invoke(() =>
            {
                tb_loop.Text = $"Running. Loops: {loops}";
            });


            _es.Run(_app =>
            {
                _app.OpenPatientById("4010090");
                _app.ClosePatient();
            });

            while (false)
            {


                string connectionString = "Server=ariaprod;Database=variansystem;User Id=reports;Password=reports;";
                string queryString = @"SELECT p.PatientId, nsa.HstryDateTime, nsa.CreationDate, vva.Expression1, vva.SubSelector, nsa.NonScheduledActivityCode, nsa.HstryDateTime, nsa.HstryUserName, nsa.DueDateTime" +
                                    "FROM NonScheduledActivity nsa, Patient p, vv_ActivityLng vva, Activity a, ActivityInstance ai" +
                                    "WHERE nsa.ActivityInstanceSer = ai.ActivityInstanceSer AND nsa.PatientSer = p.PatientSer AND ai.ActivitySer = a.ActivitySer AND a.ActivityCode = vva.LookupValue AND vva.SubSelector = 10002" +
                                    "AND nsa.ObjectStatus = N'Active' AND vva.Expression1 LIKE N'Rad Onc Contour (Do%' AND nsa.NonScheduledActivityCode = N'Completed'" +
                                    "AND nsa.HstryDateTime >{ts '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'}";

                //MessageBox.Show(queryString);
                SqlConnection connection = new SqlConnection(connectionString);

                Retry.Do(() => connection.Open(), TimeSpan.FromSeconds(30));

                //MessageBox.Show("Connection status:" + connection.ClientConnectionId.ToString() + connection.State.ToString());

                //using (connection)
                DataTable dtCloned;
                using (SqlCommand cmd = new SqlCommand(queryString, connection))
                {

                    DataTable table = new DataTable();



                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(table);
                    }
                    dtCloned = table.Clone();


                    foreach (DataRow row in table.Rows)
                    {
                        dtCloned.ImportRow(row);
                    }


                    foreach (DataRow row in dtCloned.Rows)
                    {
                        string cr = row[0].ToString();
                        try
                        {
                            if (DateTime.Now > DateTime.Parse(row[0].ToString()) && !exclusion.Contains(cr))
                            {

                                //_appl.OpenPatientById(cr);
                                //MessageBox.Show(owner);
                                MailMessage mail = new MailMessage();
                                string toMail = "eric.vanuytven@cancercare.mb.ca";
                                string buffer = $"CR# {row[3].ToString()} physics plan check is overdue (red) and has status \"Available\". Please address ASAP. Owner: {owner}";
                                mail.Body = buffer;
                                mail.Body += "  \r\n";
                                mail.Body += "  \r\n";
                                mail.Body += "  This is an OverWatch 0.1 automated email.";
                                //client.Send(mail);
                                using (StreamWriter w = File.AppendText(@"\\ariaprod\va_transfer\AriaFiles\Eric_DoNotDelete\buffer.txt"))
                                {
                                    string bufferline = DateTime.Now.ToString() + ";;;" + toMail + ";;;" + mail.Subject + ";;;" + mail.Body + ";;;";
                                    w.WriteLine(bufferline);
                                }
                                exclusion.Add(cr);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"{ex.Message} {ex.StackTrace.ToString()}");
                            return;
                        }




                    }

                    //if (buffer != "")
                    //{
                    //    mail.Body = buffer;
                    //    mail.Body += "  \r\n";
                    //    mail.Body += "  \r\n";
                    //    mail.Body += "  This is an OverWatch 0.1 automated email.";
                    //    client.Send(mail);

                    //}
                    //MessageBox.Show(buffer);
                    //mail.Body = "this is my test email body";
                    //client.Send(mail);



                }

                Thread.Sleep(300000);
                loops++;
                this.Dispatcher.Invoke(() =>
                {
                    tb_loop.Text = $"Running. Loops: {loops}";
                });
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

        }



        private void go(object sender, RoutedEventArgs e)
        {
            t1 = new Thread(new ThreadStart(daemon));
            t1.IsBackground = true;
            t1.Start();
        }
        private void stop(object sender, RoutedEventArgs e)
        {
            t1.Abort();
            tb_loop.Text = $"Stopped.";
        }
    }
    public static class Retry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }



}
