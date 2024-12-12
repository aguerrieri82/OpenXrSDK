using Android.Media;
using Android.Runtime;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XrEngine.Media
{
    public class AndroidMediaPlayer : Java.Lang.Object, IMediaPlayer, 
        MediaPlayer.IOnPreparedListener, 
        MediaPlayer.IOnErrorListener,
        MediaPlayer.IOnCompletionListener,
        AudioManager.IOnAudioFocusChangeListener
    {
        MediaPlayer _player;
        private readonly AudioManager _audioManager;
        float _volume;
        bool _isPrepared;
        bool _isPlaying;

        public AndroidMediaPlayer()
        {
            _audioManager = (AudioManager)Application.Context.GetSystemService(global::Android.Content.Context.AudioService)!;
            _player = new MediaPlayer();
            _player.SetOnPreparedListener(this);
            _player.SetOnErrorListener(this);
            _volume = 1;

        }


        public void Open(string path)
        {
            _isPrepared = false;
            _player.SetDataSource(path);
            _player.Prepare();
        }

        public void Play()
        {
            _isPlaying = true;

            if (_isPrepared)
            {
                RequestFocus();
                _player.Looping = true;
                _player.Start();
                Volume = _volume;
            }
        }

        public void Pause()
        {
            _isPlaying = false;
            _player.Pause();
        }

        public void Stop()
        {
            _isPlaying = false;
            _player.Stop();
        }

        protected void RequestFocus()
        {
            _audioManager.RequestAudioFocus(
                  new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                 .SetOnAudioFocusChangeListener(this)
                 .Build()!);
        }

        void MediaPlayer.IOnPreparedListener.OnPrepared(MediaPlayer? mp)
        {
            _isPrepared = true;
            if (_isPlaying)
                Play();
        }

        bool MediaPlayer.IOnErrorListener.OnError(MediaPlayer? mp, [GeneratedEnum] MediaError what, int extra)
        {
            _player.Reset();
            return true;
        }

        void MediaPlayer.IOnCompletionListener.OnCompletion(MediaPlayer? mp)
        {
            Debug.WriteLine("OnCompletion");
        }

        void AudioManager.IOnAudioFocusChangeListener.OnAudioFocusChange(AudioFocus focusChange)
        {
            Debug.WriteLine(focusChange);
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _player.SetVolume(value, value);
            }
        }

        public float Position
        {
            get => _player.CurrentPosition / 1000f;
            set
            {
                _player.SeekTo((int)value * 1000);
            }
        }

        public float Duration => _player.Duration / 1000f;

    }
}
