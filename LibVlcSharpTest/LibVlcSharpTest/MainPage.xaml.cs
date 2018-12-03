using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVlcSharpTest.Popups;
using LibVlcSharpTest.ViewModels;
using LibVlcSharpTest.Views;
using LibVLCSharp.Forms.Shared;
using LibVLCSharp.Shared;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;

namespace LibVlcSharpTest
{
    public partial class MainPage : ContentPage
    {
        public MediaPlayerTimeSliderView MediaPlayerTimeSlider => MediaPlayerTimeSliderProtected;

        private readonly MediaPlayerViewModel _mediaPlayerViewModel;
        private readonly List<Video> _playItems = Video.GetList();

        private int _lastRandom = -1;
        private bool _secondRun;
        
        public LibVLC LibVlc => _mediaPlayerViewModel.LibVlc;
        public MediaPlayer MediaPlayer => _mediaPlayerViewModel.MediaPlayer;

        public MainPage()
        {
            InitializeComponent();

            _mediaPlayerViewModel = new MediaPlayerViewModel();
            BindingContext = _mediaPlayerViewModel;
        }

        public void ReInitVideoOutput()
        {
            MediaPlayer?.Stop();

            _mediaPlayerViewModel.Initialize();
        }

        protected override void OnAppearing()
        {
            Debug.WriteLine("OnAppearing");

            base.OnAppearing();

            // Exit method to avoid re-subscription
            if (_secondRun) return;

            _secondRun = true;

            _mediaPlayerViewModel.Initialize();
        }

        protected override void OnDisappearing()
        {
            Debug.WriteLine("OnDisappearing");
            
            MediaPlayer?.Stop();
        }

        private void VideoViewProtected_OnLoaded(object sender, EventArgs e)
        {
            Debug.WriteLine("VideoView Loaded");
        }

        private void VideoViewProtected_OnMediaPlayerChanged(object sender, MediaPlayerChangedEventArgs e)
        {
            Debug.WriteLine("MediaPlayer Changed");

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

            MediaPlayerTimeSlider.MainPage_OnMediaPlayerChanged();
            
            LibVlc.SetDialogHandlers(DisplayError, DisplayLogin, DisplayQuestion, DisplayProgress, UpdateProgress);
        }

        private Task DisplayError(string title, string text)
        {
            return Task.CompletedTask;
        }

        private Task DisplayLogin(Dialog dialog, string title, string text, string defaultusername, bool askstore, CancellationToken token)
        {
            var authenticationFormViewModel = new AuthenticationFormViewModel
            {
                Title = title,
                Text = text,
                Username = defaultusername
            };

            Device.BeginInvokeOnMainThread(async () => {
                var page = new AuthenticationForm(authenticationFormViewModel, dialog);

                page.Disappearing += (sender, args) =>
                {
                    if (!page.IsAuthenticated)
                    {
                        MediaPlayer.Stop();
                    }
                };

                await PopupNavigation.Instance.PushAsync(page);
            });

            return Task.CompletedTask;
        }

        private Task DisplayQuestion(Dialog dialog, string title, string text, DialogQuestionType type, string canceltext, string firstactiontext, string secondactiontext, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        private Task DisplayProgress(Dialog dialog, string title, string text, bool indeterminate, float position, string canceltext, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        private Task UpdateProgress(Dialog dialog, float position, string text)
        {
            return Task.CompletedTask;
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
            
            Device.BeginInvokeOnMainThread(() => {
                VideoState.Text = e.State.ToString();
            });
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
            Debug.WriteLine("MainPage Time Changed: " + e.Time);
            
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
            
            Device.BeginInvokeOnMainThread(() =>
            {
                VideoState.Text = (e.Cache < 100) ? "Buffering... " + (int)e.Cache + "%" : MediaPlayer.Media.State.ToString();
            });

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
                MediaPlayerTimeSlider.Slider.Maximum = MediaPlayer.Length < 1 ? 1 : MediaPlayer.Length;
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
            
            VideoTitle.Text = _playItems[ri].Title;
            VideoState.Text = "";

            var media = new Media(
                LibVlc, 
                _playItems[ri].Mrl, 
                _playItems[ri].Type == Video.Types.Url ? Media.FromType.FromLocation : Media.FromType.FromPath);

            MediaPlayer.Play(media);

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

        private async void FileButton_OnClicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform == Device.Android && !await CheckPermissionsAsync()) return;

            try
            {
                FileData fileData = await CrossFilePicker.Current.PickFile();

                if (fileData == null) return;

                Debug.WriteLine("FileName: " + fileData.FileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        
        private void PlayBunnyButton_OnClicked(object sender, EventArgs e)
        {
            var media = new Media(
                LibVlc,
                "http://ttv.tiskre.com/video/BigBuckBunny.mp4",
                 Media.FromType.FromLocation);

            MediaPlayer.Play(media);

            VideoTitle.Text = "Big Buck Bunny";
        }
        
        private void GcButton_OnClicked(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
                    },
//                    new Video()
//                    {
//                        Title = "CNN International",
//                        Mrl = "http://ttv.tiskre.com:8005/channels/play?id=3089",
//                        Type = Types.Url
//                    },
//                    new Video()
//                    {
//                        Title = "ABC NEWS HD",
//                        Mrl = "http://ttv.tiskre.com:8005/channels/play?id=23462",
//                        Type = Types.Url
//                    },
//                    new Video()
//                    {
//                        Title = "Bloomberg",
//                        Mrl = "http://ttv.tiskre.com:8005/channels/play?id=8458",
//                        Type = Types.Url
//                    },
//                    new Video()
//                    {
//                        Title = "France 24",
//                        Mrl = "http://ttv.tiskre.com:8005/channels/play?id=6463",
//                        Type = Types.Url
//                    },
//                    new Video()
//                    {
//                        Title = "Deutsche Welle",
//                        Mrl = "http://ttv.tiskre.com:8005/channels/play?id=8780",
//                        Type = Types.Url
//                    }
                };
            }
        }
        
        public async Task<bool> CheckPermissionsAsync()
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage))
                    {
                        await DisplayAlert("Player", "Storage permission is needed for file picking", "OK");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Storage);

                    if (results.ContainsKey(Permission.Storage))
                    {
                        status = results[Permission.Storage];
                    }
                }

                if (status == PermissionStatus.Granted)
                {
                    return true;
                }

                if (status != PermissionStatus.Unknown)
                {
                    await DisplayAlert("Player", "Storage permission was denied.", "OK");
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
