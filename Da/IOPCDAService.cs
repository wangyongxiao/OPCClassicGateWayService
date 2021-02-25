using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitaniumAS.Opc.Client.Da;

namespace Da
{
    public interface IOPCDa{
        string[] ScanOPCDa(string host);

        IList<TreeNode> GetTreeNodes(string service);

        string StartMonitoringItems(string serviceProgId, List<string> itemIds);

        void SetItemsValueChangedCallBack(IItemsValueChangedCallBack callBack);

        void StopMonitoringItems(string serviceProgId, string groupId);

        void ReadItemsValues(string ServerID, List<string> Items, string GroupId);

        void WriteValues(string serviceProgId, string groupId, Dictionary<string, object> itemValuePairs);
    }


    public interface IItemsValueChangedCallBack {
        void ValueChangedCallBack(string group, OpcDaItemValue[] values);
    }

    public interface DebugDataCallBack{

        void DoTreeViewCallBack(MonitorItemType itemType, IList<TreeNode> trees);
        void DoEventLogCallBack(string eventLog);
    }
}
