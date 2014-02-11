using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Helpers
{
    // Ref: http://msdn.microsoft.com/en-us/library/ms979200.aspx
    public class TaskDataStore
    {
        private static Hashtable _table = new Hashtable();
        private static Hashtable _tableResults = new Hashtable();
        private static Dictionary<string, List<string>> masterChildTaskList = new Dictionary<string, List<string>>();
        static TaskDataStore() { }

        public static object GetTask(string key)
        {
            lock(_table.SyncRoot)
            {
                if (_table.ContainsKey(key))
                {
                    return _table[key];
                }
                else
                {
                    return null;
                }
                
            }
        }

        public static void SetTask(string key, object value)
        {
            lock(_table.SyncRoot)
            {
                _table.Add(key, value);
            }
        }

        public static void RemoveTask(string key)
        {
            lock(_table.SyncRoot)
            {
                _table.Remove(key);
            }

            // remove if any child taks of this task
            RemoveChildTasks(key);
        }

        public static void SetChildTask(string masterKey, string childKey)
        {
            if (masterChildTaskList.ContainsKey(masterKey))
            {
                masterChildTaskList[masterKey].Add(childKey);
            }
            else
            {
                List<string> childKeys = new List<string>();
                childKeys.Add(childKey);
                masterChildTaskList.Add(masterKey, childKeys);
            }
        }

        public static object GetTaskResult(string key)
        {
            lock (_tableResults.SyncRoot)
            {
                if (_tableResults.ContainsKey(key))
                {
                    return _tableResults[key];
                }
                else
                {
                    return null;
                }

            }
        }

        public static void SetTaskResult(string key, object value)
        {
            lock (_tableResults.SyncRoot)
            {
                _tableResults.Add(key, value);
            }
        }

        public static void RemoveResult(string key)
        {
            lock (_tableResults.SyncRoot)
            {
                _tableResults.Remove(key);
            }
        }
        public static void ClearAll()
        {
            lock(_table.SyncRoot)
            {
                _table.Clear();
            }
            lock (_tableResults.SyncRoot)
            {
                _tableResults.Clear();
            }
        }

        private static void RemoveChildTasks(string masterTaskKey)
        {
            lock (_table.SyncRoot)
            {
                if (masterChildTaskList.ContainsKey(masterTaskKey))
                {
                    var childTaskKeys = masterChildTaskList[masterTaskKey];
                    foreach (string key in childTaskKeys)
                    {
                        _table.Remove(key);
                    }

                    masterChildTaskList.Remove(masterTaskKey);
                }
            }
        }
    }
}