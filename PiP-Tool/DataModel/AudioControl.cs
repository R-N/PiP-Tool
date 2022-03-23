using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiP_Tool.DataModel
{
    internal class AudioControl : IDisposable
    {
        private AudioSessionControl2 sessionControl;
        private ProcessInfo process;
        private SimpleAudioVolume volumeControl;

        public AudioSessionControl2 SessionControl => sessionControl;
        public ProcessInfo Process => process ?? (new ProcessInfo(this.sessionControl.ProcessID));
        public SimpleAudioVolume VolumeControl => volumeControl ?? (this.SessionControl.QueryInterface<SimpleAudioVolume>());

        public int ProcessId => this.Process.ProcessId;

        public AudioControl(AudioSessionControl2 control)
        {
            this.sessionControl = control;
        }

        internal void SetMasterVolume(float volume)
        {
            this.VolumeControl.MasterVolume = volume;
            //this.VolumeControl.SetMasterVolumeLevel(volume, Guid.Empty);
        }

        internal void SetMasterVolume(double volume)
        {
            SetMasterVolume(volume);
        }
        internal float GetMasterVolume()
        {
            return this.VolumeControl.MasterVolume;
        }

        internal float MasterVolume
        {
            get => this.VolumeControl.MasterVolume;
            set => this.VolumeControl.MasterVolume = value;
        }

        public void UnregisterAudioSessionNotification(AudioSessionEvents e)
        {
            this.SessionControl.UnregisterAudioSessionNotification(e);
        }

        public void RegisterAudioSessionNotification(AudioSessionEvents e)
        {
            this.SessionControl.RegisterAudioSessionNotification(e);
        }

        public void Dispose()
        {
            this.volumeControl?.Dispose();
            this.sessionControl?.Dispose();
            this.process?.Dispose();
        }
    }
}
