using IniParser;
using IniParser.Model;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using TitaniumAS.Opc.Client.Da;

namespace Da
{
    //启动定时广播自己地址 实现网络发现功能 
    //开启TCP服务
    //请求本机还是网络
    //请求ProgId对象节点
    //请求监听节点值
    //节点定时刷新 与此同时 节点本身可能会处于监听消息状态 因此考虑隔离

    public class SocketService: IItemsValueChangedCallBack
    {
        private AppServer _server = new AppServer(new ReceiveFilterFactory());
        private IOPCDa _iOpcDa= new OPCDaImp();
        private DebugDataCallBack _debugDataCallBack = null;
        private string _iniFilePath = string.Empty;
        private System.Timers.Timer _scanTimer = new System.Timers.Timer();
        private List<TreeNode> _treeNodeCaches = new List<TreeNode>();
        private long  _exchanging = 0;
        private Dictionary<string, AppSession> _sessionDic = new Dictionary<string, AppSession>();
        public SocketService() {

            _iOpcDa.SetItemsValueChangedCallBack(this);
        }
        bool CheckFileExist()
        {
            _iniFilePath = System.IO.Path.Combine(System.Environment.CurrentDirectory, "cfg.ini");
            if (!File.Exists(_iniFilePath))
            {
                _debugDataCallBack.DoEventLogCallBack(debugInfo("cfg.ini文件不存在！"));
                return false;
            }
            return true;
        }

        bool CheckINISectionExist() {
            FileIniDataParser fileIniDataParser = new FileIniDataParser();
            IniData data = fileIniDataParser.ReadFile(_iniFilePath);
            SectionDataCollection dataCollection = data.Sections;
            if (!dataCollection.ContainsSection("scan")){
                _debugDataCallBack.DoEventLogCallBack(debugInfo("Section不存在！"));
                return false;
            }
            _debugDataCallBack.DoEventLogCallBack(debugInfo("cfg.ini文件校验通过！"));
            return true;
        }

        Dictionary<string, object>  CheckSectionParam() {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            FileIniDataParser fileIniDataParser = new FileIniDataParser();
            IniData data = fileIniDataParser.ReadFile(_iniFilePath);
            SectionDataCollection dataCollection = data.Sections;
            SectionData sectionData = dataCollection.GetSectionData("scan");
            if (sectionData != null){
                KeyDataCollection keys = sectionData.Keys;
                int _interval = 0;
                foreach (var keyItem in keys){
                    if (keyItem.KeyName.Equals("networkSegments")){
                        string networkSegments = keyItem.Value;
                        List<string> IpAddrList = networkSegments.Split('|').ToList();
                        List<string> InvalidList = new List<string>();
                        IPAddress _point = null;
                        for (int index = 0; index < IpAddrList.Count(); index++) {
                            if (!System.Net.IPAddress.TryParse(IpAddrList[index], out _point))
                            {
                                InvalidList.Add(IpAddrList[index]);
                                _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format("{0} IP格式错误", IpAddrList[index])));
                            }
                        }
                        keyValuePairs.Add("ipAddrList", IpAddrList.Except(InvalidList).ToList()); 

                    }
                    else if (keyItem.KeyName.Equals("interval")){
                        if (!int.TryParse(keyItem.Value, out _interval)){
                            _debugDataCallBack.DoEventLogCallBack(debugInfo(" interval 参数错误！"));
                        }
                        else{
                            keyValuePairs.Add("interval", _interval);
                        }
                    }
                }
            }
            return keyValuePairs;
        }

        List<string> CheckValidateIPAddress(List<string>  addressList) {
            List<string> unusedDataList = new List<string>();
            foreach (var item in addressList){
                using (Ping p = new Ping()){
                    PingReply pingReply = p.Send(item, 100);
                    if (pingReply.Status != IPStatus.Success){
                        unusedDataList.Add(item);
                        _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format("IP：{0} 无效", item.ToString())));
                    }
                }
            }
            return addressList.Except(unusedDataList).ToList();
        }

        List<TreeNode> ScanOPCClassicServer(List<string> addresses) {
            List<TreeNode> opcDaServerList = new List<TreeNode>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            string localIp = string.Empty;
            foreach (var ip in host.AddressList){
                if (ip.AddressFamily == AddressFamily.InterNetwork){
                    localIp = ip.ToString();
                    break;
                }
            }
            foreach (var usefulItem in addresses){
                string[] opcDaList = _iOpcDa.ScanOPCDa(usefulItem);
                if (opcDaList.Length > 0){

                    TreeNode node = new TreeNode();
                    node.Name = usefulItem.ToString();
                    if (usefulItem.ToString() != localIp){
                        node.NodeType = TreeNodeType.Remote;
                    }
                    List<TreeNode> childNodes = new List<TreeNode>();
                    foreach (var opcItem in opcDaList){
                        _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format("扫描成功 地址：{0} OPCClassic {1}", usefulItem.ToString(), opcItem)));
                        childNodes.Add(new TreeNode() { Name = opcItem });
                    }
                    node.Children.AddRange(childNodes);
                    opcDaServerList.Add(node);
                }
                else
                {
                    _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format("{0} 未能扫描到OPC Da Server", usefulItem.ToString())));
                }
            }
            return opcDaServerList;
        }

        List<TreeNode> ScanOPCServerData(List<TreeNode> opcServerNodes) {
            _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format(" 正在刷新节点。。。 ")));

            opcServerNodes.ForEach((service) => {
                service.Children.ForEach((opc) => {
                    IList<TreeNode> dataNodes = _iOpcDa.GetTreeNodes(opc.Name);
                    opc.Children.AddRange(dataNodes);
                });
            });

            _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format(" 刷新节点结束")));

            if (_debugDataCallBack != null){
                _debugDataCallBack.DoTreeViewCallBack(MonitorItemType.Initial, opcServerNodes);
            }

            return opcServerNodes;
        }

        int CompareIPs(byte[] ip1, byte[] ip2)
        {
            if (ip1 == null || ip1.Length != 4)
                return -1;
            if (ip2 == null || ip2.Length != 4)
                return 1;
            int comp = ip1[0].CompareTo(ip2[0]);
            if (comp == 0)
                comp = ip1[1].CompareTo(ip2[1]);
            if (comp == 0)
                comp = ip1[2].CompareTo(ip2[2]);
            if (comp == 0)
                comp = ip1[3].CompareTo(ip2[3]);
            return comp;
        }

        void IncrementIP(byte[] ip, int idx = 3)
        {
            if (ip == null || ip.Length != 4 || idx < 0)
                return;
            if (ip[idx] == 254)
            {
                ip[idx] = 1;
                IncrementIP(ip, idx - 1);
            }
            else
                ip[idx] = (byte)(ip[idx] + 1);
        }

        public void SetupCallBack(DebugDataCallBack debugDataCallBack)
        {
            _debugDataCallBack = debugDataCallBack;
            _scanTimer.Interval = 3000;//每3秒更新一次 这里只是假设3秒可以完全执行扫描点位 实际情况不一定如此 
            _scanTimer.Elapsed += (o,e) => {
                if (Interlocked.CompareExchange(ref _exchanging, 1, 0) == 0)
                {
                    if (CheckFileExist())
                    {
                        if (CheckINISectionExist())
                        {
                            //if (_scanTimer.Interval == 3000)
                            //{
                                _scanTimer.Interval = int.MaxValue;//60000;
                            //}

                            List<string> ipAddrList = (List<string>)CheckSectionParam()["ipAddrList"];
                            List<TreeNode> tempDataList = ScanOPCServerData(ScanOPCClassicServer(CheckValidateIPAddress(ipAddrList)));
                            _treeNodeCaches.Clear();
                            _treeNodeCaches.AddRange(tempDataList);
                            Interlocked.Decrement(ref _exchanging);
                        }
                    }
                }
                else
                {
                    debugDataCallBack.DoEventLogCallBack(debugInfo("本轮扫描失败，因为上一轮还未结束！"));
                }
                
            };
            _scanTimer.Start();
        }

        string debugInfo(string info) {
            return string.Format("[{0}]:{1}",System.DateTime.Now.ToLocalTime(),info);
        }

        public void Start()
        {
            _server.NewRequestReceived += NewRequestReceived;
            _server.NewSessionConnected += NewSessionConnected;
            _server.Setup(10010);
            _server.Start(); 
        }

        void NewSessionConnected(AppSession session)
        {
            if (_debugDataCallBack != null)
            {
                _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format(" 收到来自 {0} 连接", session.RemoteEndPoint.Address.ToString())));
            }
        }

        void NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            if (_debugDataCallBack != null)
            {
                _debugDataCallBack.DoEventLogCallBack(debugInfo(string.Format("收到来自 {0} 消息=>{1}", session.RemoteEndPoint.Address.ToString(), requestInfo.Body)));
            }
            ProcessRequest(session, requestInfo);
        }

        private void ProcessRequest(AppSession session, StringRequestInfo requestInfo)
        {
            int cmd = int.Parse(requestInfo.Parameters[0]);
            if (Interlocked.CompareExchange(ref _exchanging, 1, 0) == 0)
            {
                switch (cmd)
                {
                    case (int)Command.Get_Nodes_Req:
                        {
                            string json = JsonConvert.SerializeObject(_treeNodeCaches);
                            byte[] bufferList = StructUtility.Package(new Header() { Id = int.Parse(requestInfo.Key) + 1, 
                                                                                    Cmd = (int)Command.Get_Nodes_Rsp,
                                                                                    ErrorCode = 0,
                                                                                    ContentSize = json.Length }, json);
                            session.Send(bufferList, 0, bufferList.Length);
                        }
                        break;
                    case (int)Command.Start_Monitor_Nodes_Req:
                        {
                            StartMonitoringItemsReq req = JsonConvert.DeserializeObject<StartMonitoringItemsReq>(requestInfo.Body);
                            string groupId = _iOpcDa.StartMonitoringItems(req.ServiceId, req.Items);
                            if (_debugDataCallBack != null)
                            {
                                TreeNode node = new TreeNode();
                                node.Name = groupId;
                                foreach (var item in req.Items)
                                {
                                    node.Children.Add(new TreeNode() { Name = item });
                                }
                                List<TreeNode> treeNodes = new List<TreeNode>();
                                treeNodes.Add(node);
                                _debugDataCallBack.DoTreeViewCallBack(MonitorItemType.Remove, treeNodes);
                            }
                            StartMonitoringItemsRsp rsp = new StartMonitoringItemsRsp() { ServiceId = req.ServiceId, GroupId = groupId };
                            string json = JsonConvert.SerializeObject(rsp);
                            byte[] bufferList = StructUtility.Package(new Header() { Id = int.Parse(requestInfo.Key) + 1, 
                                                                                    Cmd = (int)Command.Start_Monitor_Nodes_Rsp,
                                                                                    ErrorCode = 0,
                                                                                    ContentSize = json.Length }, json);
                            session.Send(bufferList, 0, bufferList.Length);
                            _sessionDic[groupId] = session;
                        }
                        break;
                    case (int)Command.Stop_Monitor_Nodes_Req:
                        {
                            StopMonitoringItemsReq req = JsonConvert.DeserializeObject<StopMonitoringItemsReq>(requestInfo.Body);
                            _iOpcDa.StopMonitoringItems(req.ServiceId, req.Id);
                            if (_debugDataCallBack != null)
                            {
                                TreeNode node = new TreeNode();
                                node.Name = req.Id;
                                List<TreeNode> treeNodes = new List<TreeNode>();
                                treeNodes.Add(node);
                                _debugDataCallBack.DoTreeViewCallBack(MonitorItemType.Remove, treeNodes);
                            }
                            byte[] bufferList = StructUtility.Package(new Header() { Id = int.Parse(requestInfo.Key) + 1, 
                                                                                    Cmd = (int)Command.Stop_Monitor_Nodes_Rsp,
                                                                                    ErrorCode = 0,
                                                                                    ContentSize = 0 }, string.Empty);
                            session.Send(bufferList, 0, bufferList.Length);
                            if (_sessionDic.ContainsKey(req.Id))
                            {
                                _sessionDic.Remove(req.Id);
                            }
                        }
                        break;
                    case (int)Command.Read_Nodes_Values_Req:
                        {
                            ReadItemsReq req = JsonConvert.DeserializeObject<ReadItemsReq>(requestInfo.Body);

                            _iOpcDa.ReadItemsValues(req.ServiceId, req.Items, req.GroupId);

                            // Read all items of the group synchronously.
                            //OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
                            

                            //if (_debugDataCallBack != null)
                            //{
                            //    TreeNode node = new TreeNode();
                            //    node.Name = req.GroupId;
                            //    foreach (var item in req.Items)
                            //    {
                            //        node.Children.Add(new TreeNode() { Name = item });
                            //    }
                            //    List<TreeNode> treeNodes = new List<TreeNode>();
                            //    treeNodes.Add(node);
                            //    _debugDataCallBack.DoTreeViewCallBack(MonitorItemType.Remove, treeNodes);
                            //}

                            //StartMonitoringItemsRsp rsp = new StartMonitoringItemsRsp() { ServiceId = req.ServiceId, Id = groupId };
                            //string json = JsonConvert.SerializeObject(rsp);
                            //byte[] bufferList = StructUtility.Package(new Header()
                            //{
                            //    Id = int.Parse(requestInfo.Key) + 1,
                            //    Cmd = (int)Command.Start_Monitor_Nodes_Rsp,
                            //    ErrorCode = 0,
                            //    ContentSize = json.Length
                            //}, json);
                            //session.Send(bufferList, 0, bufferList.Length);
                            //_sessionDic[groupId] = session;
                        }
                        break;


                        break;
                    case (int)Command.Write_Nodes_Values_Req:
                        {
                            WriteNodesValuesReq req = JsonConvert.DeserializeObject<WriteNodesValuesReq>(requestInfo.Body);
                            _iOpcDa.WriteValues(req.ServiceId, req.Id, req.itemValuePairs);
                            if (_debugDataCallBack != null)
                            {

                                _debugDataCallBack.DoEventLogCallBack(requestInfo.Body);
                            }
                            byte[] bufferList = StructUtility.Package(new Header() { Id = int.Parse(requestInfo.Key) + 1, 
                                                                                    Cmd = (int)Command.Write_Nodes_Values_Rsp,
                                                                                    ErrorCode = 0,
                                                                                    ContentSize = 0 }, string.Empty);
                            session.Send(bufferList, 0, bufferList.Length);
                        }
                        break;
                }

                Interlocked.Decrement(ref _exchanging);
            }
            else 
            {
                byte[] bufferList = StructUtility.Package(new Header()
                {
                    Id = int.Parse(requestInfo.Key) + 1,
                    Cmd = cmd+1,
                    ErrorCode = -99,
                    ContentSize = 0
                }, string.Empty);
                session.Send(bufferList, 0, bufferList.Length);
            }
        }

        public void Stop()
        {
            _server.NewRequestReceived -= NewRequestReceived;
            _server.NewSessionConnected -= NewSessionConnected;
            _server.Stop();
        }

        public void ValueChangedCallBack(string group, OpcDaItemValue[] values)
        {
            GroupEntity entity = new GroupEntity();
            entity.Id = group;
            List<Item> collection = new List<Item>();
            values.ToList().ForEach(v=> {
                Item i = new Item();
                i.ItemId = v.Item.ItemId;
                i.Data = v.Value;
                collection.Add(i);
            });

            entity.Items = collection;
            string json = JsonConvert.SerializeObject(entity);
            byte[] bufferList = StructUtility.Package(new Header() { Id = 0, Cmd = (int)Command.Notify_Nodes_Values_Ex, ContentSize = json.Length }, json);
            _sessionDic[group].Send(bufferList, 0, bufferList.Length);
        }
    }
}
