using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Da
{
    class WriteNodesValuesReq
    {
        public string ServiceId { get; set; }
        public string GroupId { get; set; }
        public string strMd5 { get; set; }

        public Dictionary<string, object> itemValuePairs { get; set; }

        public WriteNodesValuesReq(string _serviceProgId, Dictionary<string, object> _items, string groupId, string strmd5)
        {
            ServiceId = _serviceProgId;
            GroupId = groupId;
            strMd5 = strmd5;
            itemValuePairs = _items;
        }
    }
}
