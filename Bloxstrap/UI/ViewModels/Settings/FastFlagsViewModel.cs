using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums.FlagPresets;
using System.Windows;
using Bloxstrap.UI.Elements.Settings.Pages;
using Wpf.Ui.Mvvm.Contracts;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        private Dictionary<string, object>? _preResetFlags;

        public event EventHandler? RequestPageReloadEvent;
        
        public event EventHandler? OpenFlagEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseFastFlagManager;
            set => App.Settings.Prop.UseFastFlagManager = value;
        }

        public IReadOnlyDictionary<MSAAMode, string?> MSAALevels => FastFlagManager.MSAAModes;

        public MSAAMode SelectedMSAALevel
        {
            get => MSAALevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.MSAA")).Key;
            set => App.FastFlags.SetPreset("Rendering.MSAA", MSAALevels[value]);
        }

        public IReadOnlyDictionary<RenderingMode, string> RenderingModes => FastFlagManager.RenderingModes;

        public RenderingMode SelectedRenderingMode
        {
            get => App.FastFlags.GetPresetEnum(RenderingModes, "Rendering.Mode", "True");
            set
            {
                if (value != RenderingMode.Vulkan)
                    App.Settings.Prop.FakeBorderlessFullscreen = false; // vulkan exclusive

                App.FastFlags.SetPresetEnum("Rendering.Mode", value.ToString(), "True");
            }
        }

        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        public bool GraySkyEnabled
        {
            get => App.FastFlags.GetPreset("Rendering.GraySky") == "True";
            set => App.FastFlags.SetPreset("Rendering.GraySky", value ? "True" : null);
        }

        private static readonly string[] DynamicHeadsAnimationFlags =
        {
            "DFIntAnimationLodFacsDistanceMin",
            "DFIntAnimationLodFacsDistanceMax",
            "DFIntAnimationLodFacsVisibilityDenominator",
        };

        public bool DynamicHeadsDisabled
        {
            get => App.FastFlags.GetPreset("Rendering.DynamicHeads") == "False";
            set
            {
                App.FastFlags.SetPreset("Rendering.DynamicHeads", value ? "False" : null);

                foreach (string flag in DynamicHeadsAnimationFlags)
                    App.FastFlags.SetValue(flag, value ? "0" : null);
            }
        }

        public bool CrowdPerformanceEnabled
        {
            get => App.FastFlags.GetPreset("Rendering.FRMQualityOverride") != null;
            set
            {
                if (value)
                {
                    App.FastFlags.SetPreset("Rendering.MSAA", "1");
                    App.FastFlags.SetPreset("Rendering.GraySky", "True");
                    App.FastFlags.SetPreset("Rendering.FRMQualityOverride", "3");
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.MSAA", null);
                    App.FastFlags.SetPreset("Rendering.GraySky", null);
                    App.FastFlags.SetPreset("Rendering.FRMQualityOverride", null);
                }

                OnPropertyChanged(nameof(SelectedMSAALevel));
                OnPropertyChanged(nameof(GraySkyEnabled));
                OnPropertyChanged(nameof(FRMQualityOverride));
            }
        }

        private static readonly KeyValuePair<string, string>[] LowPingFlags =
        {
            new("DFIntConnectionMTUSize", "1280"),
            new("DFIntRakNetResendBufferArrayLength", "128"),
            new("DFIntRakNetNakResendDelayMs", "10"),
            new("DFIntRakNetNakResendDelayMsMax", "100"),
            new("DFIntRakNetNakResendDelayRttPercent", "50"),
            new("DFIntClientPacketMaxDelayMs", "10"),
            new("DFIntClientPacketMaxFrameMicroseconds", "1000"),
            new("DFIntRakNetLoopMs", "1"),
        };

        public bool LowPingEnabled
        {
            get => App.FastFlags.GetValue("DFIntConnectionMTUSize") != null;
            set
            {
                foreach (var flag in LowPingFlags)
                    App.FastFlags.SetValue(flag.Key, value ? flag.Value : null);
            }
        }

        private static readonly string[] LODLevels = { "L0", "L12", "L23", "L34" };

        public bool FRMQualityOverrideEnabled
        {
            get => App.FastFlags.GetPreset("Rendering.FRMQualityOverride") != null;
            set
            {
                if (value)
                    FRMQualityOverride = 21;
                else
                    App.FastFlags.SetPreset("Rendering.FRMQualityOverride", null);

                OnPropertyChanged(nameof(FRMQualityOverride));
                OnPropertyChanged(nameof(FRMQualityOverrideEnabled));
            }
        }

        public int FRMQualityOverride
        {
            get => int.TryParse(App.FastFlags.GetPreset("Rendering.FRMQualityOverride"), out var x) ? x : 21;
            set
            {
                App.FastFlags.SetPreset("Rendering.FRMQualityOverride", value);

                OnPropertyChanged(nameof(FRMQualityOverride));
            }
        }

        public bool MeshQualityEnabled
        {
            get => App.FastFlags.GetPreset("Geometry.MeshLOD.Static") != null;
            set
            {
                if (value)
                {
                    // we enable level 3 by default
                    MeshQuality = 3;
                }
                else
                {
                    foreach (string level in LODLevels)
                        App.FastFlags.SetPreset($"Geometry.MeshLOD.{level}", null);

                    App.FastFlags.SetPreset("Geometry.MeshLOD.Static", null);
                }

                OnPropertyChanged(nameof(MeshQualityEnabled));
            }
        }

        public int MeshQuality
        {
            get => int.TryParse(App.FastFlags.GetPreset("Geometry.MeshLOD.Static"), out var x) ? x : 0;
            set
            {
                // holy..
                int clamped = Math.Clamp(value, 0, LODLevels.Length - 1);

                for (int i = 0; i < LODLevels.Length; i++)
                {
                    int lodValue = (Math.Clamp(clamped - i, 0, 3) + 1) * 250;
                    string lodLevel = LODLevels[i];

                    App.FastFlags.SetPreset($"Geometry.MeshLOD.{lodLevel}", lodValue);
                }

                App.FastFlags.SetPreset("Geometry.MeshLOD.Static", clamped);
                OnPropertyChanged(nameof(MeshQuality));
                OnPropertyChanged(nameof(MeshQualityEnabled));
            }
        }

        public bool ResetConfiguration
        {
            get => _preResetFlags is not null;

            set
            {
                if (value)
                {
                    _preResetFlags = new(App.FastFlags.Prop);
                    App.FastFlags.Prop.Clear();
                }
                else
                {
                    App.FastFlags.Prop = _preResetFlags!;
                    _preResetFlags = null;
                }

                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
