using System;
using System.ComponentModel;
using System.Windows.Media;

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using digitalOscilloscope;

namespace DataModel
{
    public class DataModel : INotifyPropertyChanged
    {
        public DataModel()
        {

        }

        private int textData1;
        public int TextData1
        {
            get => this.textData1;
            set
            {
                this.textData1 = value;
                OnPropertyChanged();
            }
        }

        private int textData2;
        public int TextData2
        {
            get => this.textData2;
            set
            {
                this.textData2 = value;
                OnPropertyChanged();
            }
        }

        private int textData3;
        public int TextData3
        {
            get => this.textData3;
            set
            {
                this.textData3 = value;
                OnPropertyChanged();
            }
        }

        private int textData4;
        public int TextData4
        {
            get => this.textData4;
            set
            {
                this.textData4 = value;
                OnPropertyChanged();
            }
        }

        private double xMax;
        public double XMax
        {
            get => this.xMax;
            set
            {
                this.xMax = value;
                OnPropertyChanged();
            }
        }

        private double xMin;
        public double XMin
        {
            get => this.xMin;
            set
            {
                this.xMin = value;
                OnPropertyChanged();
            }
        }

        private object dataMapper;
        public object DataMapper
        {
            get => this.dataMapper;
            set
            {
                this.dataMapper = value;
                OnPropertyChanged();
            }
        }

        public ChartValues<ObservablePoint> ChartValues1 { get; set; }
        public ChartValues<ObservablePoint> ChartValues2 { get; set; }
        public ChartValues<ObservablePoint> ChartValues3 { get; set; }
        public ChartValues<ObservablePoint> ChartValues4 { get; set; }
        public Func<double, string> LabelFormatter => value => value.ToString("F");

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
