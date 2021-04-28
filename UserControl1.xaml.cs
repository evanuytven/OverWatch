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
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using OverWatch.ExtensionMethods;

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

            this.Dispatcher.Invoke(() =>
            {
                tb_loop.Text = $"Running. Loops: {loops}";
            });

            List<string> processedCRs = new List<string>();
            //_es.Run(_app =>
            //{
            //    Patient pat = _app.OpenPatientById("4010090");
            //    MessageBox.Show(pat.Courses.First().PlanSetups.First().Dose.DoseMax3D.ToString());
            //    _app.ClosePatient();
            //});

            while (true)
            {


                string connectionString = "Server=ariaprod;Database=variansystem;User Id=reports;Password=reports;";
                string queryString = @"SELECT p.PatientId, nsa.HstryDateTime, nsa.CreationDate, vva.Expression1, vva.SubSelector, nsa.NonScheduledActivityCode, nsa.HstryDateTime, nsa.HstryUserName, nsa.DueDateTime " +
                                    @"FROM NonScheduledActivity nsa, Patient p, vv_ActivityLng vva, Activity a, ActivityInstance ai WHERE nsa.ActivityInstanceSer = ai.ActivityInstanceSer AND nsa.PatientSer = p.PatientSer AND ai.ActivitySer = a.ActivitySer " +
                                    @"AND a.ActivityCode = vva.LookupValue AND vva.SubSelector = 10002 AND nsa.ObjectStatus = N'Active' AND vva.Expression1 LIKE N'Rad Onc Contour (Do%' AND nsa.NonScheduledActivityCode = N'Completed' " +
                                    $"AND nsa.HstryDateTime>{{ts '{DateTime.Now.ToString("yyyy-MM-dd")}  00:00:00 '}}";





               // MessageBox.Show(queryString);
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
                        //MessageBox.Show(processedCRs.IndexOf(cr).ToString()!=-1)
                        if (processedCRs.IndexOf(cr) != -1)
                            continue;
                        
                        //MessageBox.Show(cr);
                        try
                        {
                            _es.Run(_app =>
                            {
                                Patient pat = _app.OpenPatientById(cr);
                                StructureSet SStocheck = pat.StructureSets.OrderByDescending(w => w.HistoryDateTime).First();
                                string output = CheckForBlips(SStocheck);
                                if (output != (""))
                                {
                                    MessageBox.Show($"In patient {cr}, checked structure set {SStocheck.Id}: {output}");
                                }
                                _app.ClosePatient();
                                processedCRs.Add(cr);
                            });

                            //fireEmail("test", $"CR# {row[3].ToString()} physics plan check is overdue (red) and has status \"Available\". Please address ASAP.");


                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"{ex.Message} {ex.StackTrace.ToString()}");
                            return;
                        }




                    }




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
        public int fireEmail(string subject, string body)
        {
            MailMessage mail = new MailMessage();
            string toMail = "eric.vanuytven@cancercare.mb.ca";
            mail.Body = body;
            mail.Body += "  \r\n";
            mail.Body += "  \r\n";
            //client.Send(mail);
            using (StreamWriter w = File.AppendText(@"\\ariaprod\va_transfer\AriaFiles\Eric_DoNotDelete\buffer.txt"))
            {
                string bufferline = DateTime.Now.ToString() + ";;;" + toMail + ";;;" + mail.Subject + ";;;" + mail.Body + ";;;";
                w.WriteLine(bufferline);
            }
            return 0;

        }

        public void overdue_physics_check()
        {
            //int loops = 0;
            //List<string> exclusion = new List<string>();



            //string owner;
            ////mail.Body = "this is my test email body";



            ////client.Send(mail);

            //this.Dispatcher.Invoke(() =>
            //{
            //    tb_loop.Text = $"Running. Loops: {loops}";
            //});


            ////_es.Run(_app =>
            ////{
            ////    Patient pat = _app.OpenPatientById("4010090");
            ////    MessageBox.Show(pat.Courses.First().PlanSetups.First().Dose.DoseMax3D.ToString());
            ////    _app.ClosePatient();
            ////});

            //while (true)
            //{


            //    string connectionString = "Server=ariaprod;Database=variansystem;User Id=reports;Password=reports;";
            //    string queryString = @"SELECT p.PatientId, nsa.HstryDateTime, nsa.CreationDate, vva.Expression1, vva.SubSelector, nsa.NonScheduledActivityCode, nsa.HstryDateTime, nsa.HstryUserName, nsa.DueDateTime" +
            //                        "FROM NonScheduledActivity nsa, Patient p, vv_ActivityLng vva, Activity a, ActivityInstance ai" +
            //                        "WHERE nsa.ActivityInstanceSer = ai.ActivityInstanceSer AND nsa.PatientSer = p.PatientSer AND ai.ActivitySer = a.ActivitySer AND a.ActivityCode = vva.LookupValue AND vva.SubSelector = 10002" +
            //                        "AND nsa.ObjectStatus = N'Active' AND vva.Expression1 LIKE N'Rad Onc Contour (Do%' AND nsa.NonScheduledActivityCode = N'Completed'" +
            //                        "AND nsa.HstryDateTime >{ts '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'}";

            //    MessageBox.Show(queryString);
            //    SqlConnection connection = new SqlConnection(connectionString);

            //    Retry.Do(() => connection.Open(), TimeSpan.FromSeconds(30));

            //    //MessageBox.Show("Connection status:" + connection.ClientConnectionId.ToString() + connection.State.ToString());

            //    //using (connection)
            //    DataTable dtCloned;
            //    using (SqlCommand cmd = new SqlCommand(queryString, connection))
            //    {

            //        DataTable table = new DataTable();



            //        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            //        {
            //            adapter.Fill(table);
            //        }
            //        dtCloned = table.Clone();


            //        foreach (DataRow row in table.Rows)
            //        {
            //            dtCloned.ImportRow(row);
            //        }


            //        foreach (DataRow row in dtCloned.Rows)
            //        {
            //            string cr = row[0].ToString();
            //            try
            //            {
            //                if (DateTime.Now > DateTime.Parse(row[0].ToString()) && !exclusion.Contains(cr))
            //                {

            //                    //_appl.OpenPatientById(cr);
            //                    //MessageBox.Show(owner);
            //                    MailMessage mail = new MailMessage();
            //                    string toMail = "eric.vanuytven@cancercare.mb.ca";
            //                    string buffer = $"CR# {row[3].ToString()} physics plan check is overdue (red) and has status \"Available\". Please address ASAP. Owner: {owner}";
            //                    mail.Body = buffer;
            //                    mail.Body += "  \r\n";
            //                    mail.Body += "  \r\n";
            //                    mail.Body += "  This is an OverWatch 0.1 automated email.";
            //                    //client.Send(mail);
            //                    using (StreamWriter w = File.AppendText(@"\\ariaprod\va_transfer\AriaFiles\Eric_DoNotDelete\buffer.txt"))
            //                    {
            //                        string bufferline = DateTime.Now.ToString() + ";;;" + toMail + ";;;" + mail.Subject + ";;;" + mail.Body + ";;;";
            //                        w.WriteLine(bufferline);
            //                    }
            //                    exclusion.Add(cr);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show($"{ex.Message} {ex.StackTrace.ToString()}");
            //                return;
            //            }




            //        }

            //        //if (buffer != "")
            //        //{
            //        //    mail.Body = buffer;
            //        //    mail.Body += "  \r\n";
            //        //    mail.Body += "  \r\n";
            //        //    mail.Body += "  This is an OverWatch 0.1 automated email.";
            //        //    client.Send(mail);

            //        //}
            //        //MessageBox.Show(buffer);
            //        //mail.Body = "this is my test email body";
            //        //client.Send(mail);



            //    }

            //    Thread.Sleep(300000);
            //    loops++;
            //    this.Dispatcher.Invoke(() =>
            //    {
            //        tb_loop.Text = $"Running. Loops: {loops}";
            //    });
            //}

            ////select distinct nsa.WorkFlowActiveFlag, nsa.ObjectStatus, p.PatientId, vva.Expression1, nsa.NonScheduledActivityCode
            ////from vv_ActivityLng vva, NonScheduledActivity nsa, ActivityInstance ai, Activity a, ActivityCategory ac, Patient p
            ////where nsa.ActivityInstanceSer = ai.ActivityInstanceSer
            ////and nsa.PatientSer = p.PatientSer
            ////and nsa.WorkFlowActiveFlag=1
            ////and nsa.ActivityInstanceSer = ai.ActivityInstanceSer
            ////and ai.ActivitySer = a.ActivitySer
            ////and ac.ActivityCategorySer = a.ActivityCategorySer
            ////and nsa.NonScheduledActivityCode = 'Open'
            ////and vva.Expression1 = 'Physics Plan Check'
            ////and vva.LookupValue = a.ActivityCode
            ////and ai.ObjectStatus = 'Active'
            ////and ac.DepartmentSer = 10002.00

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

        static string CheckForBlips(StructureSet ss)
        {
            //See https://stackoverflow.com/questions/39853481/is-point-inside-polygon

            int numBlips = 0;
            string blipsInfo = "";

            VMS.TPS.Common.Model.API.Image vi = ss.Image;


            foreach (Structure s in ss.Structures.Where(s => s.Id.StartsWithArray(new string[2] { "CTV", "GTV" })))
            {

                if (s.Id.Contains("z"))
                    continue;

                //loop through each image slice
                for (int i = 0; i < vi.ZSize; i++)
                {

                    VVector[][] www_lower = null;
                    VVector[][] www_upper = null;
                    if (i != 0)
                    {
                        www_lower = s.GetContoursOnImagePlane(i - 1);
                    }
                    if (i != (vi.ZSize - 1))
                    {
                        www_upper = s.GetContoursOnImagePlane(i + 1);
                    }


                    VVector[][] www = s.GetContoursOnImagePlane(i);

                    VVector[] centrepoints = new VVector[www.GetLength(0)];
                    double[] _areas = new double[www.GetLength(0)];
                    //if only ONE structure contoured on slice, look for blips
                    if (www.GetLength(0) == 1)
                        continue;


                    for (int k = 0; k < www.GetLength(0); k++) // iterate over slice contours
                    {

                        double minX = www[k][0].x;
                        double maxX = www[k][0].x;
                        double minY = www[k][0].y;
                        double maxY = www[k][0].y;
                        double sliceZ = www[k][0].z;

                        for (int j = 0; j < www[k].Length; j++) // iterate over vertices
                        {

                            if (www[k][j].x > maxX)
                                maxX = www[k][j].x;

                            if (www[k][j].x < minX)
                                minX = www[k][j].x;

                            if (www[k][j].y > maxY)
                                maxY = www[k][j].y;

                            if (www[k][j].y < minY)
                                minY = www[k][j].y;

                        }

                        _areas[k] = (maxX - minX) * (maxY - minY);
                        centrepoints[k].x = (maxX + minX) / 2.0;
                        centrepoints[k].y = (maxY + minY) / 2.0;
                        centrepoints[k].z = sliceZ;

                        //If crudely calculated area of contour is too small, call it a blip


                    } //end k loop

                    for (int m = 0; m < www.GetLength(0); m++) // iterate over slice contours
                    {
                        if (_areas[m] > 10)
                            continue;
                        bool found_enclosing = false;

                        for (int n = 0; n < www.GetLength(0); n++)
                        {
                            if (n == m) continue;
                            if (IsInPolygon(toPointList(www[n]), toPoint(centrepoints[m])))
                                found_enclosing = true;

                        }
                        // Check if blip is enclosed by adjacent slice contours
                        for (int u = 0; u < www_upper.GetLength(0); u++)
                        {
                            if (IsInPolygon(toPointList(www_upper[u]), toPoint(centrepoints[m])))
                                found_enclosing = true;

                        }
                        for (int u = 0; u < www_lower.GetLength(0); u++)
                        {
                            if (IsInPolygon(toPointList(www_lower[u]), toPoint(centrepoints[m])))
                                found_enclosing = true;

                        }






                        if (!found_enclosing)
                        {












                            VVector blipLocationUser = vi.DicomToUser(centrepoints[m], null);
                            //VVector blipLocationUser2 = vi.DicomToUser(www[n][0], null);
                            string blipX = Math.Round(blipLocationUser.x / 10.0, 2).ToString();
                            string blipY = Math.Round(blipLocationUser.y / 10.0, 2).ToString();
                            string blipZ = Math.Round(blipLocationUser.z / 10.0, 2).ToString();

                            //MessageBox.Show($"{blipY} {Math.Round(blipLocationUser2.y / 10.0, 2).ToString()}");

                            blipsInfo += $"Potential blip or hole (area: {_areas[m].ToString("F1")}) in structure {s.Id} located near: X = {blipX} cm, Y = {blipY} cm, Z = {blipZ} cm. ";

                            numBlips += 1;
                        }
                    }
                }
            }


            //set blipsMessage string to 'OK' if no blips 
            if (numBlips == 0)
                return "";
            else
                return blipsInfo;


        }

        public static bool IsInPolygon(IList<Point> vertices, Point testPoint)
        {
            if (vertices.Count < 3) return false;
            bool isInPolygon = false;
            var lastVertex = vertices[vertices.Count - 1];
            foreach (var vertex in vertices)
            {
                if (testPoint.Y.IsBetween(lastVertex.Y, vertex.Y))
                {
                    double t = (testPoint.Y - lastVertex.Y) / (vertex.Y - lastVertex.Y);
                    double x = t * (vertex.X - lastVertex.X) + lastVertex.X;
                    if (x >= testPoint.X) isInPolygon = !isInPolygon;
                }
                else
                {
                    if (testPoint.Y == lastVertex.Y && testPoint.X < lastVertex.X && vertex.Y > testPoint.Y) isInPolygon = !isInPolygon;
                    if (testPoint.Y == vertex.Y && testPoint.X < vertex.X && lastVertex.Y > testPoint.Y) isInPolygon = !isInPolygon;
                }

                lastVertex = vertex;
            }

            return isInPolygon;
        }
        public static Point toPoint(VVector inp)
        {
            Point p = new Point();
            p.X = inp.x;
            p.Y = inp.y;
            return p;
        }
        public static List<Point> toPointList(VVector[] inp)
        {
            List<Point> p = new List<Point>();
            for (int i = 0; i < inp.Length; i++)
            {
                p.Add(new Point(inp[i].x, inp[i].y));
            }
            return p;
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

    namespace ExtensionMethods
    {
        public static class MyExtensions
        {
            public static bool IsWithin(this double value, double minimum, double maximum)
            {
                if (value >= minimum && value <= maximum)
                    return true;
                else
                    return false;
            }
            public static bool ContainsArray(this string input, string[] stringArray)
            {
                foreach (string x in stringArray)
                {
                    if (input.Contains(x))
                        return true;

                }
                return false;
            }
            public static bool StartsWithArray(this string input, string[] stringArray)
            {
                foreach (string x in stringArray)
                {
                    if (input.StartsWith(x))
                        return true;

                }
                return false;
            }
            public static bool IsBetween(this double x, double a, double b)
            {
                return (x - a) * (x - b) < 0;
            }

        }
    }
}

