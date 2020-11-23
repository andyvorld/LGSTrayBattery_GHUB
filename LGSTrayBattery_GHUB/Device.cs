using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace LGSTrayBattery_GHUB
{
    class Device : INotifyPropertyChanged
    {
        #pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore 0067

        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public double Percentage { get; set; } = double.NaN;
        public double Mileage { get; set; } = double.NaN;
        public bool Charging { get; set; } = false;

        [DependsOn("DisplayName", "Percentage", "Charging")]
        public string ToolTip
        {
            get
            {
                string output = $"{DisplayName}, {Percentage:f2}%";

                if (!double.IsNaN(Percentage))
                {
                    output += Charging ? ", Charging" : "";
                }

                return output;
            }
        }

        public bool IsChecked { get; set; } = false;

        public string ToXml()
        {
            string output = "";
            output += "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
            output += "<xml>\n";
            output += $"<device_id>{DeviceId}</device_id>\n";
            output += $"<device_name>{DisplayName}</device_name>\n";
            output += $"<battery_percent>{Percentage:f2}</battery_percent>\n";
            output += $"<mileage>{Mileage:f2}</mileage>\n";
            output += $"<charging>{Charging}</charging>\n";
            output += "</xml>";

            return output;
        }
    }
}
