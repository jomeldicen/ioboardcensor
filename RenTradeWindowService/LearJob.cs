﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenTradeWindowService
{
    public class LearJob
    {
        private LearCebuPAO_API.LearCebuPAO_API OLearCebuPAOapi;
        private RegistryDriver registry;

        public string JobInfo { get; private set; }
        public string OrderNumber { get; private set; }
        public string OldOrderNos { get; private set; }
        public string LeadSet { get; private set; }
        public int JobQty { get; private set; }
        public int BundleSize { get; private set; }

        private readonly string _environmentMode;
        private readonly string _machineName;

        public LearJob(IOptions<ServiceConfiguration> options)
        {
            // Registry init/configuration
            registry = new RegistryDriver(options);

            _environmentMode = options.Value.EnvironmentMode;
            _machineName = options.Value.MachineName;

            GetJobStarted();
        }

        public void GetJobStarted()
        {
            try
            {
                JobInfo = "123456789:::10348177:::20:::10";

                // Production Mode
                if (_environmentMode == "PRD")
                {
                    JobInfo = OLearCebuPAOapi.GetJobStarted(_machineName);
                }

                string[] stringSeparators = new string[] { ":::" };
                if (!String.IsNullOrEmpty(JobInfo) && JobInfo != "ErrorDB")
                {
                    string[] tokens = JobInfo.Split(stringSeparators, StringSplitOptions.None);

                    OrderNumber = tokens[0].Trim().ToString();
                    LeadSet = tokens[1].Trim().ToString();
                    JobQty = Convert.ToInt16(tokens[2]);
                    BundleSize = Convert.ToInt16(tokens[3]);

                    registry.WriteRegistry("jobInfo", JobInfo);
                }
                else
                {
                    OrderNumber = "";
                    LeadSet = "";
                    JobQty = 0;
                    BundleSize = 0;

                    registry.ResetRegistry();
                }
            }
            catch (Exception ex)
            {
                //ErrorLogger("Get JobStarted Function: " + ex.Message);
            }
        }

        public bool IsBOMExist(string BOMValue)
        {
            string sBOMitems = "";
            bool isBOMexist = true;
            string[] stringSeparators = new string[] { ":::" };

            // Production Environment Mode
            if (_environmentMode == "PRD") 
                isBOMexist = OLearCebuPAOapi.DoesBOMexist(LeadSet, BOMValue, ref sBOMitems);

            string[] tokens = sBOMitems.Split(stringSeparators, StringSplitOptions.None);

            if (isBOMexist || tokens.Contains(BOMValue))
            {
                return true;
            }           

            return false;
        }

        public void JobFinished()
        {
            bool isPaoJobFinished = true;

            // Production Mode
            if (_environmentMode == "PRD")
                isPaoJobFinished = OLearCebuPAOapi.IsPAOJobFinished(OrderNumber, _machineName);

            if (isPaoJobFinished)
            {
                registry.WriteRegistry("oldOrderNos", OrderNumber);

                if (OrderNumber.Equals(OldOrderNos))
                    registry.WriteRegistry("quotaCounter", "0");

                registry.ResetRegistry();
            }
        }
    }
}
