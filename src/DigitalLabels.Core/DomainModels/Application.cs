using System;
using DigitalLabels.Core.Config;

namespace DigitalLabels.Core.DomainModels
{
    public class Application : DomainModel
    {
        public DateTime LastDataImport { get; private set; }

        public bool DataImportRunning { get; private set; }

        public bool DataImportCancelled { get; private set; }

        public Application()
        {
            Id = Constants.ApplicationId;
        }

        public void RunDataImport()
        {
            if (!DataImportRunning)
            {
                DataImportRunning = true;
            }
        }

        public void DataImportFinished()
        {
            DataImportRunning = false;
            DataImportCancelled = false;
        }

        public void DataImportSuccess(DateTime dateCompleted)
        {
            DataImportRunning = false;
            DataImportCancelled = false;
            LastDataImport = dateCompleted;
        }

        public void CancelDataImport()
        {
            if (DataImportRunning)
            {
                DataImportCancelled = true;
            }
        }
    }
}
