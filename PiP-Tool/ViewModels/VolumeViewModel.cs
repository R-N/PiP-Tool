using System;
using System.Linq;
using System.Windows;
using Slider = System.Windows.Controls.Slider;
using CSCore.CoreAudioAPI;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using PiP_Tool.DataModel;
using PiP_Tool.Interfaces;
using PiP_Tool.Shared;
using System.Windows.Input;
using PiP_Tool.Classes;

namespace PiP_Tool.ViewModels
{
    public class VolumeViewModel : ViewModelBase, ICloseable, IDisposable
    {
        private AudioControls audioControls;

        private double volume = 0;
        public double Volume
        {
            get => volume;
            set
            {
                volume = value;
                RaisePropertyChanged();
                //NotifyPropertyChanged("Volume");
                SetMixerVolume(value);
            }
        }

        private Slider volumeSlider;
        private Window thisWindow;
        public ICommand LoadedCommand { get; }
        //public ICommand SliderChangedCommand { get; }

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public VolumeViewModel()
        {
            Logger.Instance.Debug("   ====== VolumeDialog ======   ");

            MessengerInstance.Register<AudioControls>(this, InitAudioControls);
            LoadedCommand = new RelayCommand(LoadedCommandExecute);
            //SliderChangedCommand = new RelayCommand(SliderChangedCommandExecute);
        }

        /// <summary>
        /// Executed when the window is loaded. Get handle of the window and call <see cref="InitDwmThumbnail"/> 
        /// </summary>
        private void LoadedCommandExecute()
        {
            thisWindow = ThisWindow();
            volumeSlider = (Slider)thisWindow.FindName("VolumeSlider");
        }

        /// <summary>
        /// Gets this window
        /// </summary>
        /// <returns>This window</returns>
        private Window ThisWindow()
        {
            var windowsList = Application.Current.Windows.Cast<Window>();
            return windowsList.FirstOrDefault(window => window.DataContext == this);
        }

        /// <summary>
        /// Set selected region' data. Set position and size of this window
        /// </summary>
        /// <param name="audioControls">Audio controls of the selected window</param>
        private void InitAudioControls(AudioControls audioControls)
        {
            if (audioControls == null)
            {
                Logger.Instance.Error("Can't init Audio Controls");
                return;
            }
            this.audioControls = audioControls;
            MessengerInstance.Unregister<AudioControls>(this);

            Logger.Instance.Debug("Init Volume : " + audioControls.SelectedWindow.Title);
            this.audioControls.SessionEvents.SimpleVolumeChanged += OnMixerChanged;
            this.Volume = this.audioControls.MasterVolume;
            //this.SetSliderValue(this.audioControls.MasterVolume);
        }

        public event EventHandler<EventArgs> RequestClose;

        /// <inheritdoc />
        /// <summary>
        /// Remove DragHook
        /// </summary>
        public void Dispose()
        {
            this.audioControls.Dispose();
        }

        /// <summary>
        /// Executed on click on close button. Close this window
        /// </summary>
        private void CloseCommandExecute()
        {
            MessengerInstance.Unregister<SelectedWindow>(this);
        }

        private void SetMixerVolume(float volume)
        {
            Logger.Instance.Debug("Setting master volume to " + volume);
            this.audioControls.SetAllMasterVolume(volume);
        }

        private void SetMixerVolume(double volume)
        {
            SetMixerVolume((float)volume);
        }

        private void SetSliderValue(float value)
        {
            SetSliderValue((double)value);
        }
        private void SetSliderValue(double value)
        {
            if (volumeSlider.Value != value)
                volumeSlider.Value = value;
            /*
            if (Volume != value)
                Volume = value;
            */
        }

        private void OnMixerChanged(object sender, AudioSessionSimpleVolumeChangedEventArgs e)
        {
            Logger.Instance.Debug("Notified mixer changed to " + e.NewVolume);
            SetSliderValue(e.NewVolume);
        }

        public void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var value = e.NewValue;
            Logger.Instance.Debug("Slider changed to " + value);
            if (volume != value)
                volume = value;
            SetMixerVolume(value);
        }
    }
}
