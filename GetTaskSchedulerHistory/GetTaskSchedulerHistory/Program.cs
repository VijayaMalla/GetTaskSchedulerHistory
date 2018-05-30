using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;

namespace GetTaskSchedulerHistory
{
    class Program
    {
        public static void Main(string[] args)
        {
            //Establish a connection to the Event Log Session for the given computer name
            var computerName = "<<NAME_OF_YOUR_COMPUTER>>"; //name of your computer
            using (var session = new EventLogSession(computerName))
            {
                //getting the list of event logs from the session
                var result = GetCompletedScheduledTaskEventRecords(session);

                //Filtering the data more and getting what ever is required, you can change this code
                //to get more information or in a different format
                var response = result
                            .OrderByDescending(x => x.TimeCreated)
                            .Select(r => new
                            {
                                EventId = r.Id,
                                Publisher = r.ProviderName,
                                CompletedTime = r.TimeCreated,
                                r.TaskDisplayName,
                                Props = string.Join(" | ", r.Properties.Select(p => p.Value))
                            }).ToList();

                //Writing the response to a text file, with a delimiter which I then exported to Excel 
                string fileName = "PATH_TO_FILE_TO_WRITE_THE_LOGS.TXT";
                using (var fs = new StreamWriter(fileName))
                {
                    foreach (var item in response)
                    {
                        fs.WriteLine(item.EventId + "||" + item.TaskDisplayName + "||" + item.CompletedTime + "||" + item.Props);
                    }
                }
            }
        }

        //If you don't want completed tasks remove the second part in the where clause
        private static List<EventRecord> GetCompletedScheduledTaskEventRecords(EventLogSession session)
        {
            //Below logName is very important since out windows Task Scheduler writes logs to the below
            var logName = "Microsoft-Windows-TaskScheduler/Operational";

            var logquery =
                new EventLogQuery(logName, PathType.LogName) { Session = session };

            //the userId associated to the Tasks Scheduled in the Task Scheduler 
            var userId = "USER_ID"; //ex: S-1-5-18

            //name of the task we want to get the history for
            //we can remove this filtering from below to get logs for all tasks
            var taskName = "NAME_OF_THE_TASK_YOU_WANT_GET_HISTORY";

            //we can update the query here to only get the completed Tasks etc by 
            //adding the TaskID (ex: 102 is Task Completed, 201 is Action Completed) 
            //shown in the commented code as below
            return GetRecords(logquery, x =>
                        x.TimeCreated > DateTime.Today.AddDays(-2)
                        && x.Properties.Select(p => p.Value).Contains(taskName)
                        //&& x.Id == <<TaskId>> 
                        && x.UserId.Value == userId
                        ).ToList();
        }

        //Gets all the logs and filters them with the query.
        private static IEnumerable<EventRecord> GetRecords(EventLogQuery query, Func<EventRecord, bool> filter)
        {
            var response = new List<EventRecord>();
            using (var reader = new EventLogReader(query))
            {
                for (var record = reader.ReadEvent(); null != record; record = reader.ReadEvent())
                {
                    if (!filter(record)) continue;
                    response.Add(record);
                }
            }
            return response;
        }
    }
}
