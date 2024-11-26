using CanvasUI.Components;
using Fftw;
using Newtonsoft.Json;
using OpenAl.Framework;
using System.IO;
using System.Numerics;
using System.Windows.Controls;
using XrEditor.Services;
using XrEngine;
using XrEngine.Audio;

namespace XrEditor.Audio
{
    [Panel("c219f3ab-392d-49f6-8afc-df69d2c6d283")]
    public class LoopEditorPanel : BasePanel, IAssetEditor
    {
        public class AudioSlice
        {
            public int Start {  get; set; } 

            public int Length { get; set; }

            public Vector2[]? Frequencies { get; set; } 
        }

    
        public class Settings : BaseView, IItemView
        {
            readonly LoopEditorPanel _host;

            private float _offset;
            private float _duration;
            private float _smoothFactor;
            private float _loopEndOfs;
            private float _pitchFactor;
            private float _loopStartOfs;
            private bool _showMain;
            private bool _showSmooth;
            private bool _showLoop;
            private bool _showPitch;

            public Settings(LoopEditorPanel host)
            {
                _host = host;
            }

            protected override void OnPropertyChanged(string name)
            {
                if (_host.Plotter == null)
                    return;

                _host._version++;
                _host.UpdatePlotterView();
                _host.UpdateAudioPlot();
                _host.UpdateDftPlot();
                _host.ComputePitch();
                _host.ComputeSmooth();

                base.OnPropertyChanged(name);
            }

            [Range(0, 30, 0.01f)]
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
            public float LoopEndOffset
            {
                get => _loopEndOfs;
                set => SetProperty(ref _loopEndOfs, value);
            }

            [Range(0, 5000, 1)]
            public float LoopStartOffset
            {
                get => _loopStartOfs;
                set => SetProperty(ref _loopStartOfs, value);
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
        protected PitchShiftFilter _pitchFilter;
        protected IAudioOut _audioOut;
        protected Task? _playTask;
        protected long _version;
        protected TextView _statusText;
        protected AudioClip? _clip;
        protected AudioClip? _loopClip;
        protected AudioClip? _pitchClip;
        protected float[]? _loopClipData;


        public LoopEditorPanel()
        {
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

            _settings = new Settings(this)
            {
                Duration = 0.2f,
                SmoothFactor = 20,
                ShowMain = true,
                ShowSmooth = true
            };

            Plotter = new Plotter
            {
                MinY = -1,
                ShowAxisX = true,
                ShowAxisY = true,
                LabelWidthY = 40,
                FormatValueX = v => TimeSpan.FromSeconds(v).ToString(@"mm\:ss\.fff")
            };

            DftPlotter = new Plotter
            {
                AutoScaleY = AutoScaleYMode.Serie,
                ShowAxisX = true,
                ShowAxisY = true,
                LabelWidthY = 40,
                AutoScaleX = AutoScaleXMode.Fit,
                FormatValueX = v => MathF.Round(v).ToString()
            };

            Plotter.Tool<PanPlotterTool>().CanPanY = false;

            Plotter.ViewChanged += (_, _) =>
            {
                _settings.Offset = Plotter.ViewRect.X;
                //_settings.Duration = Plotter.ViewRect.Width;
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
            ToolBar.AddButton("icon_repeat", FindBestLoop);
            ToolBar.AddDivider();
            _statusText = ToolBar.AddText("");
            ToolBar.AddDivider();
            ToolBar.AddButton("icon_insights", () => Task.Run(GenerateLoops));
            LoadWaveAsset("CarSound.wav");
        }

        public void GenerateLoops()
        {
            _settings.Offset = 0;
            var slices = new List<AudioSlice>();

            while (_settings.Offset + _settings.Duration < _clip!.Range.Duration)
            {
                Log.Info(this, $"Computing {_settings.Offset}");

                FindBestLoop();
                UpdateDftPlot();

                var range = new AudioRange(_clip.Format);
                range.StartTime = _settings.Offset;
                range.Duration = _settings.Duration;
                range.StartSample += (int)_settings.LoopStartOffset;
                range.EndSample -= (int)_settings.LoopEndOffset;

                var best = _dft.Points.OrderByDescending(a => a.Y).Take(3);
                
                slices.Add(new AudioSlice
                {
                    Length = range.Length,
                    Start = range.StartSample,
                    Frequencies = best.ToArray()
                });

                _settings.Offset += _settings.Duration / 2;
            }

            var json = JsonConvert.SerializeObject(slices);

            _mainDispatcher.Execute(() => Context.Require<IClipboard>().Copy(json, "text/json"));

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
            _audioOut.Open(_clip!.Format);

            List<byte[]> buffers = [[], []];

            long lastVersion = -1;

            while (_playTask != null)
            {
                if (_version != lastVersion)
                {
                    //_audioOut.Dequeue(500);

                    _audioOut.Reset();

                    var curClip = _settings.ShowPitch ? _pitchClip! : _loopClip!;

                    for (var i = 0; i < buffers.Count; i++)
                    {
                        var buffer = buffers[i];

                        Array.Resize(ref buffer, curClip.Range.Size);

                        curClip.CopyTo(buffer);

                        _audioOut.Enqueue(buffer);

                        buffers[i] = buffer;
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
            _clip = new AudioClip(data.Buffer, data.Format);

            _mainAudio.Points = _clip.ToVector();
            _mainAudio.NotifyChanged();

            _version++;

            UpdatePlotterView();
            UpdateAudioPlot();
            UpdateDftPlot();
            ComputePitch();
            ComputeSmooth();
        }

        public unsafe void FindBestLoop()
        {
            var clip = _clip!.SubClipDuration(_settings.Offset, _settings.Duration);
            var clipLen = clip.Range.Length;
            var corLen = clipLen / 4;
            clip.Range.Length += corLen;
            var data = clip.ToFloat();

            fixed (float* pData = data)
            {
                //First sample that cross 0 line
                var i = 0;
                var lastValue = pData[0];
                while (i < clipLen)
                {
                    var curValue = pData[i];
                    if ((lastValue < 0 && curValue > 0) ||
                        (lastValue > 0 && curValue < 0))
                    {
                        _settings.LoopStartOffset = i;
                        break;
                    }
                    lastValue = curValue;
                    i++;
                }

                //best correlation with the end of the first 1/4 of the loop;
                var maxSum = float.MinValue;
                var maxOfs = 0;
                var testLen = (clipLen / 2) - _settings.LoopStartOffset;

                for (i = 0; i < testLen; i++)
                {
                    var sum = ComputeCorrelation(i, data, corLen, clipLen);
                    if (sum > maxSum)
                    {
                        maxSum = sum;
                        maxOfs = i;
                    }
                }

                var start = clipLen - maxOfs;
                int forward = 0;
                int backward = 0;

                //move forward to find 0 cross line
                i = start;
                lastValue = pData[i];
              
                while (i < clipLen)
                {
                    var curValue = pData[i];
                    if ((lastValue < 0 && curValue > 0) ||
                        (lastValue > 0 && curValue < 0))
                    {
                        forward = i - start;
                        break;
                    }
                    lastValue = curValue;
                    i++;
                }

                //move backward to find 0 cross line
                i = start;
                lastValue = pData[i];

                while (i > _settings.LoopStartOffset)
                {
                    var curValue = pData[i];
                    if ((lastValue < 0 && curValue > 0) ||
                        (lastValue > 0 && curValue < 0))
                    {
                        backward = start - i;
                        break;
                    }
                    lastValue = curValue;
                    i--;
                }

                //adjusft ofs based on min distance from 0-cross
                if (forward < backward)
                    maxOfs -= forward;
                else
                    maxOfs += backward;

                _settings.LoopEndOffset = maxOfs;
            }
        }

        protected float ComputeCorrelation(int endOfs, float[] data, int corLen, int clipLen)
        {
            var s1 = (int)_settings.LoopStartOffset;
            var s2 = clipLen - endOfs;

            var sum = 0f;
            for (var j = 0; j < corLen; j++)
                sum += data[s1 + j] * data[s2 + j];

            return sum;
        }

        protected void ComputeCorrelation()
        {
            var clip = _clip!.SubClipDuration(_settings.Offset, _settings.Duration);
            var clipLen = clip.Range.Length;
            var corLen = clipLen / 4;
            clip.Range.Length += corLen;
            var data = clip.ToFloat();

            var sum = ComputeCorrelation((int)_settings.LoopEndOffset, data, corLen, clipLen);

            _statusText.Text = MathF.Round(sum, 1).ToString();
        }

        protected void ComputeSmooth()
        {
            if (!_settings.ShowSmooth)
                return;

            var smooth = _settings.SmoothFactor / 1000f;

            var outData = new float[_loopClipData!.Length];

            var curSample = _loopClipData[0];

            for (var i = 0; i < _loopClipData.Length; i++)
            {
                curSample += (_loopClipData[i] - curSample) * smooth;
                outData[i] = curSample;
            }

            _smoothAudio!.Points = outData
                .Select((v, i) => new Vector2((_loopClip!.Range.StartSample + i) / (float)_loopClip.Format.SampleRate, v))
                .ToArray();

            _smoothAudio.IsVisible = _settings.ShowSmooth;
            _smoothAudio.NotifyChanged();
        }

        protected void ComputePitch()
        {
            if (!_settings.ShowPitch)
                return;

            var sampleRate = _loopClip!.Format.SampleRate;
            var len = _loopClip.Range.Length;
            var output = new float[len];

            _pitchFilter.FFtSize = len;
            _pitchFilter.Factor = (float)Math.Exp(_settings.PitchFactor / 10);
            _pitchFilter.Initialize(len, sampleRate);
            _pitchFilter.Transform(_loopClipData!, output);

            _pitchClip = AudioClip.FromFloats(output, _clip!.Format);

            _pitchAudio.Points = _pitchClip.ToVector(_loopClip.Range.StartTime);
            _pitchAudio.IsVisible = _settings.ShowPitch;
            _pitchAudio.NotifyChanged();
        }

        protected void UpdateAudioPlot()
        {
            if (_settings.Offset < 0)
                _settings.Offset = 0;

            _loopClip = _clip!.SubClipDuration(_settings.Offset, _settings.Duration);
            _loopClip.Range.EndSample -= (int)_settings.LoopEndOffset;
            _loopClip.Range.StartSample += (int)_settings.LoopStartOffset;

            _loopClipData = _loopClip.ToFloat();

            Plotter.ReferencesX.Clear();
            Plotter.ReferencesX.Add(new PlotterReference
            {
                Value = _loopClip.Range.EndTime,
                Color = "#ff0000",
                Name = "Loop End"
            });
            Plotter.ReferencesX.Add(new PlotterReference
            {
                Value = _loopClip.Range.StartTime,
                Color = "#00ff00",
                Name = "Loop Start"
            });

            Plotter.ReferencesY.Clear();
            Plotter.ReferencesY.Add(new PlotterReference
            {
                Value = 0,
                Color = "#ffffff",
                Name = "Zero"
            });

            if (_settings.ShowLoop)
            {
                _loopAudio!.Points = _loopClip.SubClipDuration(_loopClip.Range.EndTime, _loopClip.Range.Duration).ToVector(-_loopClip.Range.Duration);
                _loopAudio.IsVisible = _settings.ShowLoop;
                _loopAudio.NotifyChanged();
            }

            _mainAudio.IsVisible = _settings.ShowMain;
            _mainAudio.NotifyChanged();

            ComputeCorrelation();
        }


        protected unsafe void UpdateDftPlot()
        {
            var dftSize = (int)(MathF.Round(_loopClip!.Range.Length / 32) * 32);

            using var aIn = new FftwBuffer<double>(dftSize);
            using var aOut = new FftwBuffer<Complex>(dftSize / 2 + 1);

            for (var j = 0; j < dftSize; j++)
                aIn.Pointer[j] = _loopClipData![j];

            FftwLib.Dft(aIn, aOut);

            var freqStep = (float)_loopClip.Format.SampleRate / dftSize;

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
            UpdateAudioPlot();
            UpdateDftPlot();
        }

        public bool IsPlaying => _playTask != null;

        public Plotter Plotter { get; }

        public Plotter DftPlotter { get; }

        public override string? Title => "Loop Editor";
    }
}
