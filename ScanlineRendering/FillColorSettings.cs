using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ScanlineRendering
{
    public class FillColorSettings: DependencyObject, INotifyPropertyChanged
    {
        private bool kMSliders, normalMap, interpolMode, hybridMode,
            colorFromTexture, movingLight, nogrid;

        public bool KMSliders
        {
            get
            {
                return kMSliders;
            }
            set
            {
                if (value != kMSliders)
                {
                    kMSliders = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool NormalMap
        {
            get
            {
                return normalMap;
            }
            set
            {
                if (value != normalMap)
                {
                    normalMap = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool InterpolMode
        {
            get
            {
                return interpolMode;
            }
            set
            {
                if (value != interpolMode)
                {
                    interpolMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool HybridMode
        {
            get
            {
                return hybridMode;
            }
            set
            {
                if (value != hybridMode)
                {
                    hybridMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool ColorFromTexture
        {
            get
            {
                return colorFromTexture;
            }
            set
            {
                if (value != colorFromTexture)
                {
                    colorFromTexture = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool MovingLight
        {
            get
            {
                return movingLight;
            }
            set
            {
                if (value != movingLight)
                {
                    movingLight = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool NoGrid
        {
            get
            {
                return nogrid;
            }
            set
            {
                if (value != nogrid)
                {
                    nogrid = value;
                    RaisePropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public FillColorSettings(bool km, bool n, bool interpol, bool hybrid, bool io, bool l, bool nogrid = false)
        {
            KMSliders = km;
            NormalMap = n;
            InterpolMode = interpol;
            HybridMode = hybrid;
            ColorFromTexture = io;
            MovingLight = l;
            NoGrid = nogrid;
        }
    }
}
