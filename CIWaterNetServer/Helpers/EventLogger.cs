using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

namespace UWRL.CIWaterNetServer.Helpers
{
    public enum EventID
    {
        Error = 101,
        Success = 200
    }
    public class EventLogger
    {
        public static void Log(string message, EventLogEntryType eventType, EventID eventID)
        {
            const string applicationName = "CIWaterNetService";
            // Create an instance of EventLog
            EventLog eventLog = new EventLog();

            // Check if the event source exists. If not create it.
            if (!System.Diagnostics.EventLog.SourceExists(applicationName))
            {
                EventLog.CreateEventSource(applicationName, "Application");
            }

            // Set the source name for writing log entries.
            eventLog.Source = applicationName;
                        
            // Write an entry to the event log.
            eventLog.WriteEntry(message, eventType,  (int)eventID);

            // Close the Event Log
            eventLog.Close();
        }

    }
}