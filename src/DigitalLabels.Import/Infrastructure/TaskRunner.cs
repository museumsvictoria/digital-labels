using System;
using System.Collections.Generic;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using Raven.Client;
using Serilog;

namespace DigitalLabels.Import.Infrastructure
{
    public class TaskRunner
    {
        private readonly IEnumerable<ITask> tasks;
        private readonly IDocumentStore store;

        public TaskRunner(
            IEnumerable<ITask> tasks,
            IDocumentStore store)
        {
            this.tasks = tasks;
            this.store = store;
        }

        public void ExecuteAll()
        {
            using (Log.Logger.BeginTimedOperation("Tasks starting", "TaskRunner.RunAllTasks"))
            {
                var tasksFailed = false;
                var session = store.OpenSession();
                var application = session.Load<Application>(Constants.ApplicationId);

                if (!application.TasksRunning)
                {
                    application.ExecuteTasks();

                    try
                    {
                        foreach (var importTask in tasks)
                        {
                            if (Program.ImportCanceled)
                                break;

                            importTask.Execute();
                        }
                    }
                    catch (Exception ex)
                    {
                        tasksFailed = true;
                        Log.Logger.Error(ex, "Exception occured running export");
                    }

                    session = store.OpenSession();
                    application = session.Load<Application>(Constants.ApplicationId);

                    if (Program.ImportCanceled || tasksFailed)
                    {
                        Log.Logger.Information("Tasks have been stopped prematurely {@Reason}", new { Program.ImportCanceled, tasksFailed });
                        application.TasksComplete();
                    }
                    else
                    {
                        Log.Logger.Information("All tasks finished successfully");
                        application.TasksSuccessful(DateTime.Now);
                    }
                }
                else
                {
                    Log.Logger.Information("Task runner is already running... stopping");
                }
            }
        }
    }

}
