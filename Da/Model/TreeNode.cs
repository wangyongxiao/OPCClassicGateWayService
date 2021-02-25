using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Da
{
    public enum TreeNodeType
    {
        Root,
        Local,
        Remote,
        Property
    }

    public class TreeNode
    {
        public string Name { get; set; }
        [JsonIgnore]
        public TreeNodeType NodeType { get; set; }


        public List<TreeNode> Children { get; set; }

        public TreeNode()
        {
            Children = new List<TreeNode>();
            NodeType = TreeNodeType.Local;
        }

    }
}
