using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceProcess;
using System.Text;
using log4net;

namespace Domain
{
    public class Machine
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Machine));

        private string _name;

        private MachineStatus _status;

        private DateTime? _downTimeStamp;
        private DateTime? _unreachableTimeStamp;

        private bool _restartAttempted;

        public Machine(string name)
        {
            _name = name;   
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public MachineStatus Status
        {
            get
            {
                return _status;
            }
        }


        public void CheckStatus()
        {
            try
            {
                ServiceController serviceController = new ServiceController("Spooler", _name);

                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    if (_status != MachineStatus.Up)
                    {
                        _log.Info(string.Format("\tPrint Spooler Back Up on {0}!", _name));
                    }

                    _downTimeStamp = null;
                    _unreachableTimeStamp = null;
                    _restartAttempted = false;
                    _status = MachineStatus.Up;
                }

                if (serviceController.Status != ServiceControllerStatus.Running)
                {                    
                    if (_status == MachineStatus.Dead)
                    {
                        if (_restartAttempted)
                            return;

                        TimeSpan span = DateTime.Now.Subtract(_downTimeStamp.Value);
                        if (span.Seconds > int.Parse(ConfigurationManager.AppSettings["AttemptRestartAfter"]))
                            AttemptRestart();

                    }
                    else if (_status == MachineStatus.Down)
                    {
                        _status = MachineStatus.Dead;
                    }
                    else
                    {
                        _log.Info(string.Format("\tPrint Spooler Down on {0}!", _name));
                        _downTimeStamp = DateTime.Now;
                        _status = MachineStatus.Down;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Info(string.Format("Failed to reach {0} with the following error: ", _name), ex);
                _unreachableTimeStamp = DateTime.Now;
                _status = MachineStatus.Unreachable;
            }


        }

        private void AttemptRestart()
        {
            
            try
            {
                _restartAttempted = true;

                ServiceController serviceController = new ServiceController("Spooler", _name);

                if (serviceController.Status != ServiceControllerStatus.Running)
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running);
                    CheckStatus();
                }
            }
            catch (Exception ex)
            {
                _log.Info(string.Format("Failed to restart {0} with the following error: ", _name), ex);
                
            }
        }
    }
}
