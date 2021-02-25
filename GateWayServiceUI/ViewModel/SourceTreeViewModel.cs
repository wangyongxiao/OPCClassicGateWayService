using System.Collections.ObjectModel;

namespace GateWayServiceUI.ViewModel
{
    internal class SourceTreeViewModel
    {
        private ObservableCollection<TreeViewNode> _sources = new ObservableCollection<TreeViewNode>();
        public ObservableCollection<TreeViewNode>  Sources
        {
            get
            {
                return _sources;
            }
        }

        public SourceTreeViewModel()
        {
            
        }

    }
}
