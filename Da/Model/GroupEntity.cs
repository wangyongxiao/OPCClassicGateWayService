using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Da
{
    class GroupEntity
    {
        public string Id { get;set;}

        public List<Item> Items { get;set;}
        
    }

    public class Item
    {

        public string ItemId { get; set; }

        public object Data { get; set; }
    }
}
