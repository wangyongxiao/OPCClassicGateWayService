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
        public string strMd5 { get; set; }
        public List<string> Items { get; set; }

        public StartMonitoringItemsReq(string _serviceProgId, List<string> _items, string strmd5)
        {
            ServiceId = _serviceProgId;
            Items = _items;
            strMd5 = strmd5;
        }
    }
}
