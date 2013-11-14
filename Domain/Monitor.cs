using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Timers;
using System.Configuration;
using log4net;

namespace Domain
{


    public class Monitor
    {
        private const string FileName = "MachineNames.txt";
        private DateTime? _fileLastChanged;


        private static readonly ILog _log = LogManager.GetLogger(typeof (Monitor));
        private readonly Timer _timer;

        private IList<Machine> _machineList;

        public Monitor()
        {            
            int waitTime;
            if (!int.TryParse(ConfigurationManager.AppSettings["TimeBetweenChecks"], out waitTime))
                waitTime = 60;

            _timer = new Timer();
            _timer.Interval = waitTime * 1000; //convert wait time to seconds.
            _timer.AutoReset = true;
            _timer.Elapsed += DoWork;

            ReadFile();
        }

        private void ReadFile()
        {
            try
            {
                using (TextReader reader = new StreamReader(FileName))
                {
                    _machineList = new List<Machine>();

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            _machineList.Add(new Machine(line));
                        }
                    }
                }

                _fileLastChanged = File.GetLastWriteTime(FileName);
            }
            catch (Exception ex)
            {
                _log.Info("Reading MachineNames.txt failed with the following error: ", ex);
            }
        }

        private void CheckFile()
        {
            DateTime? lastChanged = File.GetLastWriteTime(FileName);
            if (lastChanged > _fileLastChanged)
            {
                ReadFile();
            }
        }


        public void Run()
        {
            _timer.Enabled = true;
        }

        public void Kill()
        {
            _timer.Enabled = false;
        }


        private void DoWork(object sender, ElapsedEventArgs e)
        {
            //stop timer so event does not fire if method is still doing work.
            _timer.Enabled = false;

            _log.Info("******Checking Spooler Start******");

            CheckFile();

            try
            {
                foreach (Machine machine in _machineList)
                {
                   machine.CheckStatus();
                }                

                _log.Info(string.Format("Total Up: {0}, Total Down: {1}, Total Unreachable: {2}", MachinesUp.Count, MachinesDown.Count + MachinesDead.Count, MachinesUnreachable.Count));

                SendEmail(MachinesDown);
                

            }
            catch (Exception ex)
            {
                _log.Info(string.Format("An error has been encounted while checking the print spool service. What follows is the error for debuging reasons: \n{0}", ex.Message));
            }
            finally
            {
                _log.Info("******Checking Spooler End******\n\n");
                _timer.Enabled = true;
            }
        }

        public IList<Machine> MachinesUp
        {
            get
            {
                IList<Machine> list = new List<Machine>();
                foreach (Machine machine in _machineList)
                {
                    if (machine.Status == MachineStatus.Up)
                        list.Add(machine);
                }

                return list;
            }
        }

        public IList<Machine> MachinesDown
        {
            get
            {
                IList<Machine> list = new List<Machine>();
                foreach (Machine machine in _machineList)
                {
                    if (machine.Status == MachineStatus.Down)
                        list.Add(machine);
                }

                return list;
            }
        }

        public IList<Machine> MachinesDead
        {
            get
            {
                IList<Machine> list = new List<Machine>();
                foreach (Machine machine in _machineList)
                {
                    if (machine.Status == MachineStatus.Dead)
                        list.Add(machine);
                }

                return list;
            }
        }

        public IList<Machine> MachinesUnreachable
        {
            get
            {
                IList<Machine> list = new List<Machine>();
                foreach (Machine machine in _machineList)
                {
                    if (machine.Status == MachineStatus.Unreachable)
                        list.Add(machine);
                }

                return list;
            }
        }

        private void SendEmail(IList<Machine> machinesDown)
        {
            try
            {

                if (machinesDown.Count == 0)
                    return;

                SmtpClient mySmtpClient = new SmtpClient(ConfigurationManager.AppSettings["SmtpServer"],
                                                         int.Parse(ConfigurationManager.AppSettings["SmtpPort"]));


                if (bool.Parse(ConfigurationManager.AppSettings["SmptAuth"]))
                {

                    // set smtp-client with basicAuthentication
                    mySmtpClient.UseDefaultCredentials = false;
                    System.Net.NetworkCredential basicAuthenticationInfo = new
                        System.Net.NetworkCredential(ConfigurationManager.AppSettings["SmtpUser"],
                                                     ConfigurationManager.AppSettings["SmtpPassword"]);
                    mySmtpClient.Credentials = basicAuthenticationInfo;
                    mySmtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["SmptSsl"]);
                }
                
               MailMessage myMail = new MailMessage();                
               myMail.From = new MailAddress("DoNotReply@DoNotReply.com", "DO NOT REPLY");

               IList<string> mailToList = ConfigurationManager.AppSettings["EmailTo"].Split(';');
                foreach(string mailTo in mailToList)
                {
                    myMail.To.Add(new MailAddress(mailTo));
                }

                // set subject and encoding
                myMail.Subject = "Print Spools Down";                

                // set body-message and encoding
                myMail.Body = "The following machines have Print Spoolers that are down:\n\n";

                foreach (Machine machine in machinesDown)
                {
                    myMail.Body += machine.Name + "\n";   
                }
                
                mySmtpClient.Send(myMail);
            }
            catch (Exception ex)
            {
                _log.Info("Failed to send email with following error: ", ex);
            }
        }             
    }
}
