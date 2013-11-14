using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using Domain;

namespace Service
{
    public partial class Service : ServiceBase
    {
        private Monitor _monitor;

        public Service()
        {
            InitializeComponent();
            _monitor = new Monitor();
        }

        protected override void OnStart(string[] args)
        {
            _monitor.Run();
        }

        protected override void OnStop()
        {
            _monitor.Kill();
        }
        
    }
}
