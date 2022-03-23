using System;
using System.Diagnostics;
using System.Management;
using PiP_Tool.Native;
using PiP_Tool.Shared;

namespace PiP_Tool.DataModel
{
    public class ProcessInfo : IDisposable
    {

        #region public

        public int ProcessId { get; private set; }
        public System.Diagnostics.Process Process { get; private set; }
        public string Program { get; private set; }
        private int parentProcessId = -1;
        public int ParentProcessId => parentProcessId == -1 ? (parentProcessId = this.GetParentProcessId()) : parentProcessId;
        private ProcessInfo parentProcess = null;
        public ProcessInfo ParentProcess => parentProcess ?? (parentProcess = this.GetParentProcess());

        public string Title { get; private set; }


        #endregion

        #region private


        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handle">Handle of the window</param>
        public ProcessInfo(int processId, string title=null)
        {
            this.ProcessId = processId;
            this.Title = title;
            RefreshInfo();
        }
        public ProcessInfo(WindowInfo window) : this(window.ProcessId, window.Title)
        {
        }

        /// <summary>
        /// Refresh all window informations (size, position, title, style, border...)
        /// </summary>
        public void RefreshInfo()
        {
            GetProcess();
            GetProgram();
            if (this.Process != null)
                this.Title = this.Process.MainWindowTitle;
            // GetParentProcessId();
        }

        /// <summary>
        /// Get window process
        /// </summary>
        private System.Diagnostics.Process GetProcess()
        {
            if (ProcessId <= 0)
                return null;
            this.Process = System.Diagnostics.Process.GetProcessById(ProcessId);
            return this.Process;
        }


        /// <summary>
        /// Get window program name
        /// </summary>
        private String GetProgram()
        {
            if (Process != null)
                Program = this.Process.ProcessName;
            return Program;
        }

        /// <summary>
        /// Check if obj if is WindowInfo and compare handle
        /// </summary>
        /// <param name="obj">object to compare</param>
        /// <returns>Handles are equals</returns>
        public override bool Equals(object obj)
        {
            return obj is ProcessInfo processInfo && ProcessId.Equals(processInfo.ProcessId);
        }

        /// <summary>
        /// Compare handle
        /// </summary>
        /// <param name="other">WindowInfo to compare</param>
        /// <returns>Handles are equals</returns>
        protected bool Equals(WindowInfo other)
        {
            return ProcessId.Equals(other.ProcessId);
        }

        /// <summary>
        /// Get hashcode of the Handle
        /// </summary>
        /// <returns>Hashcode of the Handle</returns>
        public override int GetHashCode()
        {
            return ProcessId.GetHashCode();
        }

        /// <summary>
        /// Equality operator override
        /// </summary>
        /// <param name="left">Left member of the comparison</param>
        /// <param name="right">Right member of the comparison</param>
        /// <returns>Handles are equals</returns>
        public static bool operator ==(ProcessInfo left, ProcessInfo right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Inequality operator override
        /// </summary>
        /// <param name="left">Left member of the comparison</param>
        /// <param name="right">Right member of the comparison</param>
        /// <returns>Handles are not equals</returns>
        public static bool operator !=(ProcessInfo left, ProcessInfo right)
        {
            return !Equals(left, right);
        }

        static ProcessInfo GetProcessById(int pid)
        {
            if (pid <= 0)
                return null;
            try
            {
                return new ProcessInfo(pid);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        internal int GetParentProcessId()
        {
            var parentProcessId = 0;
            try
            {
                parentProcessId = this.GetParentProcessId1();
            }
            catch (ArgumentException)
            {
                
            }
            this.parentProcessId = parentProcessId;
            Logger.Instance.Info("Child-Parent: " + this.ProcessId + " " + this.ParentProcessId);
            return ParentProcessId;
        }
        internal int GetParentProcessId1()
        {
            var performanceCounter = new PerformanceCounter("Process", "Creating Process ID", this.Program);
            return (int)performanceCounter.RawValue;
        }

        internal int GetParentProcessId2()
        {
            int parentProcessId = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + this.ProcessId.ToString() + "'"))
            {
                mo.Get();
                return parentProcessId = Convert.ToInt32(mo["ParentProcessId"]);
            }
        }
        internal int GetParentProcessId3()
        {
            return ProcessExtensions.ParentProcessId(this.ProcessId);
        }

        internal ProcessInfo GetParentProcess()
        {
            if (ParentProcessId == 0)
                return null;
            this.parentProcess = GetProcessById(ParentProcessId);
            return ParentProcess;
        }

        internal ProcessInfo GetRootProcess()
        {
            var parent = this.GetParentProcess();
            if (parent == null)
                return this;
            else
                return parent.GetParentProcess();
        }

        public void Dispose()
        {
            this.Process?.Dispose();
            this.parentProcess?.Dispose();
        }
    }
}
