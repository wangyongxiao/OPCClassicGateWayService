using System.Collections.Generic;

namespace Da
{
    public class ReadItemsReq
    {
        public string ServiceId { get; set; }
        public string GroupId { get; set; }
        public string strMd5 { get; set; }
        public List<string> Items { get; set; }

        public ReadItemsReq(string _serviceProgId, List<string> _items, string groupId, string strmd5)
        {
            ServiceId = _serviceProgId;
            GroupId = groupId;
            strMd5 = strmd5;
            Items = _items;
        }
    }
}
