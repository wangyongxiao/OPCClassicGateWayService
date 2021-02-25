using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitaniumAS.Opc.Client.Da;

namespace Da
{
    public class OpcDaService
    {
        public string Host { get;set;}

        public string ServiceId { get;set;}

        public OpcDaServer Service { get; set; }

        public Dictionary<String, OpcDaGroup> OpcDaGroupS;
    }
}
