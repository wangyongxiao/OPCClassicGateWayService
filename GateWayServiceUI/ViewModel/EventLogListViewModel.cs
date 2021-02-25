using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GateWayServiceUI.ViewModel
{
    internal class EventLogListViewModel 
    {
        private ObservableCollection<string> _sources = new ObservableCollection<string>();
        public ObservableCollection<string> Sources
        {
            get
            {
                return _sources;
            }
        }
    }
}
