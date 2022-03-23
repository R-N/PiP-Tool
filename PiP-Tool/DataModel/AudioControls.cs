using CSCore.CoreAudioAPI;
using PiP_Tool.DataModel;
using PiP_Tool.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiP_Tool.Classes
{
    internal class AudioControls : IDisposable
    {
        private ProcessInfo selectedWindow;
        public ProcessInfo SelectedWindow
        {
            get { return selectedWindow; }
        }
        private AudioControl[] audioControls = new AudioControl[0];
        private AudioSessionEvents sessionEvents = new AudioSessionEvents();

        public bool HasControls
        {
            get
            {
                return audioControls.Length > 0;
            }
        }
        public AudioSessionEvents SessionEvents
        {
            get { return sessionEvents; }
        }
        public AudioControls(ProcessInfo selectedWindow)
        {
            this.selectedWindow = selectedWindow;
        }
        public AudioControls(WindowInfo selectedWindow) : this(new ProcessInfo(selectedWindow))
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove DragHook
        /// </summary>
        public void Dispose()
        {
            DisposeControls();
        }

        private void DisposeControls()
        {
            foreach (AudioControl audioControl in this.audioControls)
            {
                if (audioControl == null)
                    continue;
                audioControl.UnregisterAudioSessionNotification(this.sessionEvents);
                audioControl.Dispose();
            }
            this.audioControls = new AudioControl[0];
        }
        private List<AudioControl> GetControls(DataFlow dataFlow, Func<AudioSessionControl2, bool> cond)
        {
            List<AudioControl> audioControls = new List<AudioControl>();
            using (var enumerator = new MMDeviceEnumerator())
            {
                foreach (MMDevice device in enumerator.EnumAudioEndpoints(dataFlow, DeviceState.Active))
                {
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                    {
                        foreach (var session in sessionEnumerator)
                        {
                            var sessionControl = session.QueryInterface<AudioSessionControl2>();
                            if (cond(sessionControl))
                            {
                                audioControls.Add(new AudioControl(sessionControl));
                            }
                            else
                            {
                                sessionControl.Dispose();
                            }
                        }
                    }
                }
            }
            return audioControls;
        }

        internal void GetControls(DataFlow dataFlow)
        {
            var audioControls0 = GetControls(dataFlow, x => (
                x.ProcessID == this.selectedWindow.ProcessId
                || x.Process.MainWindowTitle == this.selectedWindow.Title
                || x.Process.ProcessName == this.selectedWindow.Program
            ));

            if (audioControls0.Count == 0)
            {
                Logger.Instance.Debug("Found no audio control");
                DisposeControls();
                return;
            }

            /*
            if (audioControls0.Count == 0) {
                Logger.Instance.Debug("Getting controls by root");
                audioControls0 = GetControls(dataFlow, x => (
                    new ProcessInfo(x.ProcessID).GetRootProcess().ProcessId == this.selectedWindow.GetRootProcess().ProcessId
                ));
                Logger.Instance.Debug("Getting controls by root done");
            }
            */

            var audioControls2 = audioControls0.Where(x => (
                x.ProcessId == this.selectedWindow.ProcessId
            )).ToList();
            if (audioControls2.Count == 0)
            {
                audioControls2 = audioControls0.Where(x => (
                    x.Process.Program == this.selectedWindow.Program
                    && x.Process.Title == this.selectedWindow.Title
                )).ToList();
            }
            /*
            if (audioControls2.Count == 0)
            {
                Logger.Instance.Debug("Filtering controls by root");
                audioControls2 = audioControls0.Where(x => (
                    x.Process.Program == this.selectedWindow.Program
                    && x.Process.GetRootProcess().ProcessId == this.selectedWindow.GetRootProcess().ProcessId
                )).ToList();
                Logger.Instance.Debug("Filtering controls by root done");
            }
            */
            if (audioControls2.Count == 0) { 
                audioControls2 = audioControls0.Where(x => (
                    x.Process.Program == this.selectedWindow.Program
                )).ToList();
            }
            /*
            if (audioControls2.Count == 0)
            {
                Logger.Instance.Debug("Filtering controls by root2");
                audioControls2 = audioControls0.Where(x => (
                    x.Process.GetRootProcess().ProcessId == this.selectedWindow.GetRootProcess().ProcessId
                )).ToList();
                Logger.Instance.Debug("Filtering controls by root2 done");
            }
            */

            if (audioControls2.Count == 0)
            {
                Logger.Instance.Debug("Found no audio control");
                DisposeControls();
                return;
            }
            DisposeControls();
            this.audioControls = audioControls2.ToArray();
            Logger.Instance.Debug("Found " + audioControls.Length + " audio controls");
            foreach(AudioControl control in this.audioControls)
            {
                control.RegisterAudioSessionNotification(sessionEvents);
                Logger.Instance.Debug("AudioController " + control.Process.Program + " " + control.Process.Title);
            }
        }
        internal void SetAllMasterVolume(float volume)
        {
            foreach (AudioControl control in this.audioControls)
            {
                if (control.MasterVolume != volume) {
                    control.MasterVolume = volume;
                    Logger.Instance.Debug("Notified mixer changed to " + control.MasterVolume);
                }
            }
        }

        internal void SetAllMasterVolume(double volume)
        {
            SetAllMasterVolume((float)volume);
        }
        internal float GetMaxMasterVolume()
        {
            return this.audioControls.Max(x => x.MasterVolume);
        }

        internal float MasterVolume
        {
            get { return GetMaxMasterVolume();  }
            set { SetAllMasterVolume(value);  }
        }
    }


}
