using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Da
{
    public class StartMonitoringItemsReq
    {
        public string ServiceId { get; set; }
        public List<string> Items { get; set; }

        public StartMonitoringItemsReq(string _serviceId, List<string> _items)
        {
            ServiceId = _serviceId;
            Items = _items;
        }

    }
}
