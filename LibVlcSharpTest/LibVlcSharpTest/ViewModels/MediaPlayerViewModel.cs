using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LibVLCSharp.Shared;

namespace LibVlcSharpTest.ViewModels
{
    public class MediaPlayerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private LibVLC _libVlc;
        private MediaPlayer _mediaPlayer;

        public LibVLC LibVlc
        {
            get => _libVlc;
            set => SetProperty(ref _libVlc, value);
        }

        public MediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
            set => SetProperty(ref _mediaPlayer, value);
        }

        public void Initialize()
        {
            Core.Initialize();

            LibVlc = new LibVLC();
            Debug.WriteLine("LibVlc Created");

            MediaPlayer = new MediaPlayer(LibVlc) { EnableKeyInput = true };
            Debug.WriteLine("MediaPlayer Created");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }
    }
}
