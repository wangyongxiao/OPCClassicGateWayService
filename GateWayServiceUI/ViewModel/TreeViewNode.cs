
using Da;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GateWayServiceUI.ViewModel
{
    public class TreeViewNode: INotifyPropertyChanged
    {
        public string Name
        {
            get
            {
                return _node.Name;
            }
            set
            {
                _node.Name = value;
                OnPropertyChanged("Name");
            }
        }

        public TreeNodeType NodeType
        {
            get
            {
                return _node.NodeType;
            }
            set
            {
                _node.NodeType = value;
                OnPropertyChanged("NodeType");
            }
        }

        private TreeNode _node = null;

        public TreeViewNode(TreeNode node)
        {
            _node = node;
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
            }
        }

        private ObservableCollection<TreeViewNode> _children = new ObservableCollection<TreeViewNode>();
        public ObservableCollection<TreeViewNode> Children
        {
            get
            {
                return _children;
            }
        }

        public void AppendNode(TreeViewNode node)
        {
            _children.Add(node);
        }

        public void DeleteNode(TreeViewNode node)
        {
            _children.Remove(node);
        }

       
        public void ClearNodes()
        {
            _children.Clear();
        }

        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
