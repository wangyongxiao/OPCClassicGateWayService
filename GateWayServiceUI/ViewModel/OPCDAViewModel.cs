using Da;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GateWayServiceUI.ViewModel
{
    internal class OPCDAViewModel: DebugDataCallBack
    {
        private SocketService _server = new SocketService();

        private SourceTreeViewModel _sourceTree;
        public SourceTreeViewModel SourceTree
        {
            get
            {
                return _sourceTree;
            }
        }

        private EventLogListViewModel _eventList;
        public EventLogListViewModel EventList
        {
            get
            {
                return _eventList;
            }
        }

        public OPCDAViewModel() {

            _sourceTree = new SourceTreeViewModel();
            _eventList = new EventLogListViewModel();
            //_server.SetupCallBack(this);
            //_server.Start();
        }

        public void Start()
        {
            _server.SetupCallBack(this);
            _server.Start();
        }
        public void DoTreeViewCallBack(MonitorItemType itemType, IList<TreeNode> trees)
        {
            if (itemType == MonitorItemType.Initial)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() => {
                    SourceTree.Sources.Clear();
                    var _root = new TreeViewNode(new TreeNode());
                    _root.Name = "OPC-DA-Service";
                    _root.NodeType = TreeNodeType.Root;
                    TreeViewNode localNode = new TreeViewNode(new TreeNode() { Name = "Local Computer", NodeType = TreeNodeType.Local });
                    _root.AppendNode(localNode);

                    TreeViewNode lanNode = new TreeViewNode(new TreeNode() { Name = "Lan", NodeType = TreeNodeType.Remote });
                    _root.AppendNode(lanNode);
                    SourceTree.Sources.Add(_root);

                    foreach (var item in trees)
                    {
                        Recursion(item.NodeType==TreeNodeType.Remote? lanNode : localNode, item);
                    }

                }));
                
            }
        }

        void Recursion(TreeViewNode viewNode, TreeNode childNode)
        {
            var viewItem = new TreeViewNode(childNode);
            viewNode.AppendNode(viewItem);
            if (childNode.Children.Any())
            {
                var children = childNode.Children.ToList();
                children.ForEach((child)=> {
                    Recursion(viewItem, child);
                });
               
            }

        }


        public void DoEventLogCallBack(string eventLog)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(()=> {
                _eventList.Sources.Add(eventLog);
            }));
           
        }
    }
}
