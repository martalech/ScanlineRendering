using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ScanlineRendering
{
    public class TrianglesInfo: DependencyObject, INotifyPropertyChanged
    {
        private string n, m;

        public TrianglesInfo(string n, string m)
        {
            N = n;
            M = m;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string N
        {
            get
            {
                return n;
            }
            set
            {
                if (value != n)
                {
                    n = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string M
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

        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
