using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibVlcSharpTest.Views;
using LibVLCSharp.Shared;
using Xamarin.Forms;

namespace LibVlcSharpTest
{
    public partial class MainPage : ContentPage
    {
        public MediaPlayer MediaPlayer => videoView.MediaPlayer;
        public MediaPlayerTimeSliderView MediaPlayerTimeSlider => MediaPlayerTimeSliderProtected;

        private readonly List<Video> _playItems = Video.GetList();

        private int _lastRandom = -1;
        private bool _secondRun;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Exit method to avoid re-subscription
            if (_secondRun) return;

            _secondRun = true;

            MediaPlayer.MediaChanged += MediaPlayer_MediaChanged;
            MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            MediaPlayer.Opening += MediaPlayer_Opening;
            MediaPlayer.Buffering += MediaPlayer_Buffering;
            MediaPlayer.Playing += MediaPlayer_Playing;
            MediaPlayer.Paused += MediaPlayer_Paused;
            MediaPlayer.Stopped += MediaPlayer_Stopped;
            MediaPlayer.EndReached += MediaPlayer_EndReached;
            MediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            MediaPlayer.LengthChanged += MediaPlayer_LengthChanged;

            MediaPlayerTimeSlider.MainPage_OnAppearing();
        }

        private void MediaPlayer_MediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            Debug.WriteLine("Media Changed: " + MediaPlayer.Media.Mrl);

            MediaPlayer.Media.StateChanged += Media_StateChanged;
            MediaPlayer.Media.DurationChanged += Media_DurationChanged;
            MediaPlayer.Media.MetaChanged += Media_MetaChanged;

            MediaStopped();
        }

        private void Media_StateChanged(object sender, MediaStateChangedEventArgs e)
        {
            Debug.Write("Media State: " + e.State);
        }

        private void Media_DurationChanged(object sender, MediaDurationChangedEventArgs e)
        {
            Debug.WriteLine("Media Duration: " + e.Duration);
        }

        private void Media_MetaChanged(object sender, MediaMetaChangedEventArgs e)
        {
            Debug.WriteLine("Media Meta");
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Debug.WriteLine("MainPage (I will be shown twice) Time Changed: " + e.Time);
            
            Device.BeginInvokeOnMainThread(() => {
                MediaPlayerTime.Text = LongToTime(MediaPlayer.Time);
            });
        }
        
        private void MediaPlayer_Opening(object sender, EventArgs e)
        {
            MediaPlaying();
        }

        private void MediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            Debug.WriteLine("Buffering: " + e.Cache);

            MediaPlaying();
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            Debug.WriteLine("Playing");

            MediaPlaying();
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            Debug.WriteLine("Paused");

            Device.BeginInvokeOnMainThread(() => {
                PlayButton.Text = "Play";
            });
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            Debug.WriteLine("Stopped");

            MediaStopped();
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            Debug.WriteLine("End Reached");
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            Debug.WriteLine("Encountered Error");
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Debug.WriteLine("Length Changed: " + e.Length);
            
            Device.BeginInvokeOnMainThread(() => {
                MediaPlayerTimeSlider.Slider.Maximum = MediaPlayer.Length;
                MediaPlayerTimeSlider.Slider.IsEnabled = true;

                MediaPlayerLength.Text = LongToTime(MediaPlayer.Length);
            });
        }

        private void PlayButton_OnClicked(object sender, EventArgs e)
        {
            if (MediaPlayer.Media == null)
            {
                PlayRandomButton_OnClicked(null, null);
            }
            else if (IsPlaying())
            {
                if (MediaPlayer.Media.State == VLCState.Playing)
                {
                    MediaPlayer.Pause();
                }
                else
                {
                    MediaPlayer.Play();
                }
            }
            else
            {
                MediaPlayer.Play(MediaPlayer.Media);
            }
        }

        private void PlayRandomButton_OnClicked(object sender, EventArgs e)
        {
            var r = new Random();
            var ri = r.Next(0, _playItems.Count);

            while (_lastRandom == ri)
            {
                ri = r.Next(0, _playItems.Count);
            }

            _lastRandom = ri;

            var media = new Media(
                videoView.LibVLC, 
                _playItems[ri].Mrl, 
                _playItems[ri].Type == Video.Types.Url ? Media.FromType.FromLocation : Media.FromType.FromPath);

            MediaPlayer.Play(media);

            VideoTitle.Text = _playItems[ri].Title;
        }

        private void StopButton_OnClicked(object sender, EventArgs e)
        {
            MediaPlayer.Stop();
        }

        private void MediaPlaying()
        {
            Device.BeginInvokeOnMainThread(() => {
                PlayButton.Text = "Pause";
            });
        }

        private void MediaStopped()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                PlayButton.Text = "Play";

                MediaPlayerTime.Text = "--:--";
                MediaPlayerLength.Text = "--:--";

                MediaPlayerTimeSlider.Slider.IsEnabled = false;
                MediaPlayerTimeSlider.Slider.Maximum = 1;
                MediaPlayerTimeSlider.Slider.Value = 0;
            });
        }

        private bool IsPlaying()
        {
            VLCState[] playingStates = {
                VLCState.Opening,
                VLCState.Buffering,
                VLCState.Playing,
                VLCState.Paused
            };

            return MediaPlayer?.Media != null && playingStates.Contains(MediaPlayer.Media.State);
        }

        private static string LongToTime(long value)
        {
            TimeSpan t;

            try
            {
                t = TimeSpan.FromMilliseconds(value);
            }
            catch (Exception)
            {
                value = 0;
                t = TimeSpan.FromMilliseconds(value);
            }

            return value >= 3600 * 1000 ? $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes:D2}:{t.Seconds:D2}";
        }

        public class Video
        {
            public enum Types
            {
                File,
                Url
            }

            public string Title { get; set; }
            public string Mrl { get; set; }
            public Types Type { get; set; }

            public static List<Video> GetList()
            {
                return new List<Video>()
                {
                    new Video()
                    {
                        Title = "Big Buck Bunny",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "Elephant Dream",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "For Bigger Blazes",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "For Bigger Escape",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "For Bigger Fun",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerFun.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "For Bigger Joyrides",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyrides.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "For Bigger Meltdowns",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerMeltdowns.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "Sintel",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "Subaru Outback On Street And Dirt",
                        Mrl =
                            "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/SubaruOutbackOnStreetAndDirt.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "Tears of Steel",
                        Mrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "Volkswagen GTI Review",
                        Mrl =
                            "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/VolkswagenGTIReview.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "We Are Going On Bullrun",
                        Mrl =
                            "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WeAreGoingOnBullrun.mp4",
                        Type = Types.Url
                    },
                    new Video()
                    {
                        Title = "What care can you get for a grand?",
                        Mrl =
                            "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WhatCarCanYouGetForAGrand.mp4",
                        Type = Types.Url
                    }
                };
            }
        }
    }
}
