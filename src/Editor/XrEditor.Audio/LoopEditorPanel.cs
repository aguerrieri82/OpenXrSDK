using CanvasUI.Components;
using Fftw;
using OpenAl.Framework;
using System.Dynamic;
using System.IO;
using System.Numerics;
using System.Windows.Controls;
using UI.Binding;
using XrEditor.Services;
using XrEngine;
using XrEngine.Audio;

namespace XrEditor.Audio
{
    [Panel("LoopEditor")]
    public class LoopEditorPanel : BasePanel, IAssetEditor
    {
    
        public class Settings : BaseView, INotifyPropertyChanged, IItemView
        {
            readonly LoopEditorPanel _host;

            private float _offset;
            private float _duration;
            private float _smoothFactor;
            private float _loopOfs;
            private float _pitchFactor;
            private bool _showMain;
            private bool _showSmooth;
            private bool _showLoop;
            private bool _showPitch;

            public Settings(LoopEditorPanel host)
            {
                _host = host;
            }

            public void NotifyPropertyChanged(IProperty property)
            {
                _host._version++;
                _host.UpdatePlotterView();
                _host.UpdateAudioView();
                _host.UpdateDftView();
                _host.ComputePitch();
            }


            [Range(0, 30, 0.1f)]
            public float Offset
            {
                get => _offset;
                set => SetProperty(ref _offset, value);
            }

            [Range(0, 2, 0.01f)]
            public float Duration
            {
                get => _duration;
                set => SetProperty(ref _duration, value);
            }

            [Range(0, 1000, 0.1f)]
            public float SmoothFactor
            {
                get => _smoothFactor;
                set => SetProperty(ref _smoothFactor, value);
            }

            [Range(0, 5000, 1)]
            public float LoopOfs
            {
                get => _loopOfs;
                set => SetProperty(ref _loopOfs, value);
            }

            [Range(1, 100, 0.1f)]
            public float PitchFactor
            {
                get => _pitchFactor;
                set => SetProperty(ref _pitchFactor, value);
            }

            public bool ShowMain
            {
                get => _showMain;
                set => SetProperty(ref _showMain, value);
            }

            public bool ShowSmooth
            {
                get => _showSmooth;
                set => SetProperty(ref _showSmooth, value);
            }

            public bool ShowLoop
            {
                get => _showLoop;
                set => SetProperty(ref _showLoop, value);
            }

            public bool ShowPitch
            {
                get => _showPitch;
                set => SetProperty(ref _showPitch, value);
            }

            string IItemView.DisplayName => "Loop Editor";

            IconView? IItemView.Icon => null;
        }

        protected Settings _settings;
        protected DiscretePlotterSerie _mainAudio;
        protected DiscretePlotterSerie _loopAudio;
        protected DiscretePlotterSerie _smoothAudio;
        protected DiscretePlotterSerie _pitchAudio;
        protected DiscretePlotterSerie _dft;
        protected AudioLooper _looper;
        protected AudioSlicer _slicer;
        protected PitchShiftFilter _pitchFilter;
        protected IAudioOut _audioOut;
        protected Task? _playTask;
        protected long _version;
        protected TextView _statusText;

        public LoopEditorPanel()
        {
            _looper = new AudioLooper();
            _slicer = new AudioSlicer();
            _pitchFilter = new PitchShiftFilter();
            _audioOut = new Win32WaveOut();

            _mainAudio = new DiscretePlotterSerie()
            {
                Color = "#00ff00",
                FormatValue = y => MathF.Round(y, 1).ToString(),
            };

            _loopAudio = new DiscretePlotterSerie()
            {
                Color = "#FFFF00",
                FormatValue = y => MathF.Round(y, 1).ToString(),
                Points = []
            };

            _smoothAudio = new DiscretePlotterSerie()
            {
                Color = "#FF0000",
                FormatValue = y => MathF.Round(y, 1).ToString(),
                Points = []
            };

            _pitchAudio = new DiscretePlotterSerie()
            {
                Color = "#FF0000",
                FormatValue = y => MathF.Round(y, 1).ToString(),
                Points = []
            };

            _dft = new DiscretePlotterSerie()
            {
                Color = "#00FFFF",
                FormatValue = y => MathF.Round(y, 2).ToString(),
                Points = []
            };

            _settings = new Settings(this);
            _settings.Duration = 0.2f;
            _settings.SmoothFactor = 20;
            _settings.ShowMain = true;
            _settings.ShowSmooth = true;

            Plotter = new Plotter
            {
                MinY = -1,
                ShowAxisX = true,
                ShowAxisY = true,
                FormatValueX = v => TimeSpan.FromSeconds(v).ToString(@"mm\:ss\.fff")
            };

            DftPlotter = new Plotter
            {
                AutoScaleY = AutoScaleYMode.Serie,
                ShowAxisX = true,
                ShowAxisY = true,
                AutoScaleX = AutoScaleXMode.Fit,
                FormatValueX = v => MathF.Round(v).ToString()
            };

            Plotter.Tool<PanPlotterTool>().CanPanY = false;

            Plotter.ViewChanged += (_, _) =>
            {
                _settings.Offset = Plotter.ViewRect.X;
                _settings.Duration = Plotter.ViewRect.Width;
            };

            Plotter.Series.Add(_mainAudio);
            Plotter.Series.Add(_loopAudio);
            Plotter.Series.Add(_smoothAudio);
            Plotter.Series.Add(_pitchAudio);

            DftPlotter.Series.Add(_dft);

            ToolBar = new ToolbarView();

            ToolBar.AddButton("icon_play_arrow", PlayAsync);
            ToolBar.AddButton("icon_stop", StopAsync);
            ToolBar.AddDivider();
            ToolBar.AddButton("icon_repeat", UpdateLoopPos);
            ToolBar.AddDivider();
            _statusText = ToolBar.AddText("");

            LoadWaveAsset("CarSound.wav");
        }


        public Task PlayAsync()
        {
            if (_playTask != null)
                return _playTask;

            _playTask = Task.Run(PlayWork);

            OnPropertyChanged(nameof(IsPlaying));

            return _playTask;

        }

        protected void PlayWork()
        {
            _audioOut.Open(_looper.Loop!.Format);

            var buffers = new List<byte[]>
                {
                    ([]),
                    ([])
                };

            long lastVersion = -1;

            while (_playTask != null)
            {
                if (_version != lastVersion)
                {
                    _audioOut.Reset();

                    if (_settings.ShowPitch)
                    {
                        var data = _pitchAudio.Points.Select(a => (short)(a.Y * short.MaxValue)).ToArray();

                        for (var i = 0; i < buffers.Count; i++)
                        {
                            var buffer = buffers[i];

                            Array.Resize(ref buffer, data.Length * 2);

                            Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);

                            _audioOut.Enqueue(buffer);

                            buffers[i] = buffer;
                        }
                    }
                    else
                    {
                        var startByte = _looper.Loop!.Format.TimeToSampleByte(_settings.Offset);

                        var endByte = _looper.Loop!.Format.TimeToSampleByte(_settings.Offset + _settings.Duration) - (int)(_settings.LoopOfs * 2);

                        if (startByte < 0 || endByte < 0)
                            continue;

                        for (var i = 0; i < buffers.Count; i++)
                        {
                            var buffer = buffers[i];

                            Array.Resize(ref buffer, (endByte - startByte));

                            Buffer.BlockCopy(_looper.Loop.Buffer, startByte, buffer, 0, buffer.Length);

                            _audioOut.Enqueue(buffer);

                            buffers[i] = buffer;
                        }

                    }


                    lastVersion = _version;
                }

                var nextBuf = _audioOut.Dequeue(500);

                if (nextBuf != null)
                    _audioOut.Enqueue(nextBuf);
            }

            _audioOut.Close();
        }

        public async Task StopAsync()
        {
            if (_playTask != null)
            {
                var task = _playTask;
                _playTask = null;
                await task;
            }

            OnPropertyChanged(nameof(IsPlaying));
        }

        public void LoadWaveAsset(string path)
        {
            var asset = Context.Require<IAssetStore>();
            var soundPath = asset.GetPath(path);
            var reader = new WavReader();
            using var stream = File.OpenRead(soundPath);
            var data = reader.Decode(stream);
            Load(data);
        }

        public void Load(AudioData data)
        {
            var floats = data.ToFloat();

            _mainAudio.Points = floats
                .Select((a, i) => new Vector2(i / (float)data.Format.SampleRate, a))
                .ToArray();

            _mainAudio.NotifyChanged();

            _looper.Loop = data;
            _slicer.Data = data;
            _version++;

            UpdatePlotterView();
            UpdateAudioView();
            UpdateDftView();
            ComputePitch();
        }

        public void UpdateLoopPos()
        {
            _settings.LoopOfs = _slicer.BestLoopOffset(_settings.Offset, _settings.Duration);
        }

        protected void ComputeCorrelation()
        {
            var sampleRate = _slicer.Data!.Format.SampleRate;

            var startSample = (int)(sampleRate * _settings.Offset);
            var endSample = (int)(sampleRate * (_settings.Offset + _settings.Duration));

            var len = (endSample - startSample) + 1;

            float sum = 0;
            for (int j = 0; j < len; j++)
            {
                var s1 = endSample - (int)_settings.LoopOfs + j;
                var s2 = startSample + j;

                sum += _mainAudio!.Points[s1].Y * _mainAudio.Points[s2].Y;
            }

            _statusText.Text = MathF.Round(sum, 1).ToString();
        }

        protected void ComputePitch()
        {
            var sampleRate = _slicer.Data!.Format.SampleRate;

            var len = (int)(_settings.Duration * sampleRate) - (int)_settings.LoopOfs;
            var startSample = (int)(sampleRate * _settings.Offset);

            float[] input = _mainAudio.Points.Skip(startSample).Take(len).Select(a => a.Y).ToArray();
            float[] output = new float[len];

            _pitchFilter.FFtSize = len;
            _pitchFilter.Factor = (float)Math.Exp(_settings.PitchFactor / 10);
            _pitchFilter.Initialize(len, sampleRate);
            _pitchFilter.Transform(input, output);

            _pitchAudio.Points = output
                .Select((a, i) => new Vector2((startSample + i) / (float)sampleRate, a))
                .ToArray();
            _pitchAudio.IsVisible = _settings.ShowPitch;
            _pitchAudio.NotifyChanged();

        }

        protected void UpdateAudioView()
        {
            if (_settings.Offset < 0)
                _settings.Offset = 0;

            var sampleRate = (float)_slicer.Data!.Format.SampleRate;

            var ofsTime = _settings.LoopOfs / (float)sampleRate;

            var loopStart = _settings.Offset + _settings.Duration - ofsTime;

            var startIndex = (int)(loopStart * sampleRate);
            var endIndex = (int)((loopStart + _settings.Duration) * sampleRate);

            var data = new Vector2[endIndex - startIndex];
            for (var i = 0; i < data.Length; i++)
            {
                var p = _mainAudio!.Points[i + startIndex];
                data[i] = new Vector2(p.X - (loopStart - _settings.Offset), p.Y);
            }

            _loopAudio!.Points = data;
            _loopAudio.IsVisible = _settings.ShowLoop;
            _loopAudio.NotifyChanged();

            Plotter.ReferencesX.Clear();
            Plotter.ReferencesX.Add(new PlotterReference
            {
                Value = _settings.Offset + _settings.Duration - ofsTime,
                Color = "#ff0000",
                Name = "Loop"
            });

            Plotter.ReferencesY.Clear();
            Plotter.ReferencesY.Add(new PlotterReference
            {
                Value = 0,
                Color = "#ffffff",
                Name = "Zero"
            });

            startIndex = (int)(_settings.Offset * sampleRate);
            endIndex = (int)((_settings.Offset + _settings.Duration) * sampleRate);

            data = new Vector2[endIndex - startIndex];
            float lastY = _mainAudio!.Points[startIndex].Y;
            float smooth = _settings.SmoothFactor / 1000f;
            for (var i = 0; i < data.Length; i++)
            {
                var p = _mainAudio!.Points[i + startIndex];
                lastY += (p.Y - lastY) * smooth;
                //lastY = (lastY * _settings.SmoothFactor) + (p.Y * (1 - _settings.SmoothFactor));
                data[i] = new Vector2(p.X, lastY);
            }

            _smoothAudio!.Points = data;
            _smoothAudio.IsVisible = _settings.ShowSmooth;
            _smoothAudio.NotifyChanged();

            _mainAudio.IsVisible = _settings.ShowMain;
            _mainAudio.NotifyChanged();

            ComputeCorrelation();

        }

        protected unsafe void UpdateDftView()
        {
            var sampleRate = (float)_slicer.Data!.Format.SampleRate;
            var startIndex = (int)(_settings.Offset * sampleRate);
            var endIndex = (int)((_settings.Offset + _settings.Duration) * sampleRate);

            var dftSize = (int)(MathF.Round((endIndex - startIndex) / 32) * 32);

            using var aIn = new FftwBuffer<double>(dftSize);
            using var aOut = new FftwBuffer<Complex>(dftSize / 2 + 1);

            for (var j = 0; j < dftSize; j++)
                aIn.Pointer[j] = _mainAudio!.Points[startIndex + j].Y;

            FftwLib.Dft(aIn, aOut);

            var freqStep = (float)sampleRate / dftSize;

            var maxLen = Math.Min(aOut.Length, (int)(4000f / freqStep));

            var array = new Vector2[maxLen];

            for (var i = 0; i < maxLen; i++)
                array[i] = new Vector2(i * freqStep, (float)Math.Log10(aOut.Pointer[i].Magnitude));

            _dft.Points = array;
            _dft.NotifyChanged();
        }

        protected void UpdatePlotterView()
        {
            Plotter.ViewRect = new XrMath.Rect2(_settings.Offset, -1, _settings.Duration, 2);
        }

        public override void OnActivate()
        {
            var toolProps = Context.Require<PanelManager>().Panels
                 .OfType<PropertiesEditor>()
                 .FirstOrDefault(a => a.Mode == PropertiesEditorMode.Custom);

            if (toolProps != null)
            {
                toolProps.ActiveNode = _settings.GetNode();
                toolProps.IsActive = true;
            }
 

            UpdatePlotterView();
            UpdateAudioView();
            UpdateDftView();
        }

        public bool IsPlaying => _playTask != null;


        public Plotter Plotter { get; }

        public Plotter DftPlotter { get; }

        public override string? Title => "Loop Editor";
    }
}
