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
        public string Id { get; set; }
        public Dictionary<string, object> itemValuePairs { get; set; }
    }
}
