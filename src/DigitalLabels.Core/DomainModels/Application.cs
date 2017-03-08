using System;
using DigitalLabels.Core.Config;

namespace DigitalLabels.Core.DomainModels
{
    public class Application : DomainModel
    {
        public DateTime? LastCompleted { get; private set; }

        public bool TasksRunning { get; private set; }

        public bool TasksCancelled { get; private set; }

        public Application()
        {
            Id = Constants.ApplicationId;
        }

        public void ExecuteTasks()
        {
            if (!TasksRunning)
            {
                TasksRunning = true;
            }
        }

        public void TasksComplete()
        {
            TasksRunning = false;
            TasksCancelled = false;
        }

        public void TasksSuccessful(DateTime completed)
        {
            TasksRunning = false;
            TasksCancelled = false;
            LastCompleted = completed;
        }

        public void CancelTasks()
        {
            if (TasksRunning)
            {
                TasksCancelled = true;
            }
        }
    }
}
