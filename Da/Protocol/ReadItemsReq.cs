using System.Collections.Generic;

namespace Da
{
    public class ReadItemsReq
    {
        public string ServiceId { get; set; }
        public string GroupId { get; set; }
        public List<string> Items { get; set; }

        public ReadItemsReq(string _serviceProgId, List<string> _items, string groupId)
        {
            ServiceId = _serviceProgId;
            GroupId = groupId;
            Items = _items;
        }
    }
}
