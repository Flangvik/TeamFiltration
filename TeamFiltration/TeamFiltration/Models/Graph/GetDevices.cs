using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{


    public class GetDevices
    {
        public List<DeviceObject> deviceObjects { get; set; }
    }

    public class DeviceObject
    {
        public bool accountEnabled { get; set; }
        public string deviceId { get; set; }
        public int deviceVersion { get; set; }
        public string displayName { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string operatingSystemVersion { get; set; }
    }

}
