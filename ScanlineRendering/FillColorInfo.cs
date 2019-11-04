using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ScanlineRendering
{
    public class FillColorInfo: DependencyObject, INotifyPropertyChanged
    {
        private double kd, ks, m;
        private Color objectColor, lightColor;

        public double Kd
        {
            get
            {
                return kd;
            }
            set
            {
                if (value != kd)
                {
                    kd = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double Ks
        {
            get
            {
                return ks;
            }
            set
            {
                if (value != ks)
                {
                    ks = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double M
        {
            get
            {
                return m;
            }
            set
            {
                if (value != m)
                {
                    m = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Color ObjectColor
        {
            get
            {
                return objectColor;
            }
            set
            {
                if (value != objectColor)
                {
                    objectColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Color LightColor
        {
            get
            {
                return lightColor;
            }
            set
            {
                if (value != lightColor)
                {
                    lightColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public FillColorInfo(double ks, double kd, double m, Color Io, Color Il)
        {
            Ks = ks;
            Kd = kd;
            M = m;
            ObjectColor = Io;
            LightColor = Il;
        }
    }
}
