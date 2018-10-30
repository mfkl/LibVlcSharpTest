using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LibVlcSharpTest.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MediaPlayerTimeSliderView : ContentView
    {
        private MainPage MainPage => (MainPage)Application.Current.MainPage;

        public Slider Slider => SliderProtected;

        private bool _secondRun;
        private bool _timeSliderValueChangedOutside;

        public MediaPlayerTimeSliderView ()
        {
            InitializeComponent ();
        }

        public void MainPage_OnAppearing()
        {
            if (_secondRun) return;

            _secondRun = true;
            
            MainPage.MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Debug.WriteLine("MediaPlayerTimeSliderView (I will be shown twice) Time Changed: " + e.Time);

            _timeSliderValueChangedOutside = true;
            
            Device.BeginInvokeOnMainThread(() => {
                Slider.Value = e.Time;
            });
        }

        private void MediaPlayerTimeSlider_OnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_timeSliderValueChangedOutside)
            {
                _timeSliderValueChangedOutside = false;
                return;
            }

            if (MainPage.MediaPlayer.IsSeekable)
            {
                MainPage.MediaPlayer.Time = (int)e.NewValue;
            }
        }
    }
}