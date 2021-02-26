using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitaniumAS.Opc.Client.Da;

namespace Da
{
    public class ReadItemsRsp
    {
        public string ServiceId { get; set; }
        public string GroupId { get; set; }
        public string strMd5 { get; set; }
        public List<Item> ItemValues { get; set; }
    }
}
