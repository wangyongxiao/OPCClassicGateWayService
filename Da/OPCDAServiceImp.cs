using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TitaniumAS.Opc.Client.Common;
using TitaniumAS.Opc.Client.Da;
using TitaniumAS.Opc.Client.Da.Browsing;

namespace Da
{
    //不在一开始读取节点结构时创建缓存的原因：用户不一定会全部都读取所有节点属性值 仅仅在需要关注的时候 才读取 
    public class OPCDaImp : IOPCDa
    {
        private OpcServerEnumeratorAuto _serverEnumerator = new OpcServerEnumeratorAuto();
        private Dictionary<string, OpcDaGroup> _daGroupKeyPairs = new Dictionary<string, OpcDaGroup>();
        private List<ServiceCollection> _serviceCollection = new List<ServiceCollection>();
        private IItemsValueChangedCallBack _callBack;
        private List<OpcDaService> OpcDaServices = new List<OpcDaService>();

        public string[] ScanOPCDa(string host)
        {
            string[] retValue = new string[] { };
            try
            {

                OpcServerDescription[] opcServers = _serverEnumerator.Enumerate(host, OpcServerCategory.OpcDaServer10,
                                                                            OpcServerCategory.OpcDaServer20,
                                                                            OpcServerCategory.OpcDaServer30);

                string [] serviceList = opcServers.Select(a => a.ProgId).ToArray();
                if (serviceList.Any()) {
                    if (_serviceCollection.Any(a => { return a.Host == host; }))
                    {
                        ServiceCollection item = _serviceCollection.Where(a => { return a.Host == host; })
                                                                   .FirstOrDefault();
                        var exceptList = item.ServiceIds.Except(serviceList);
                        if (exceptList.Any()) {

                            item.ServiceIds.AddRange(exceptList);
                        }

                    }
                    else {

                        _serviceCollection.Add(new ServiceCollection() { Host = host, ServiceIds = serviceList.ToList() });
                    }
                }
                return opcServers.Select(a => a.ProgId).ToArray();
            }
            catch (Exception exp) {
                return retValue;
            }
        }

        public OpcDaService GetOpcDaService(string serviceProgId)
        {
            var service = _serviceCollection.Where(a => a.ServiceIds.Contains(serviceProgId))
                      .FirstOrDefault();

            OpcDaService Service = null;
            if (CheckServiceExisted(service, serviceProgId))
            {
                Service = OpcDaServices.Find(item => { return item.Host == service.Host && item.ServiceId == serviceProgId; });
            }
            else
            {
                OpcDaServer daService = new OpcDaServer(serviceProgId, service.Host);
                
                Service = new OpcDaService() { Host = service.Host, 
                                                ServiceId = serviceProgId, 
                                                Service = daService, 
                                                OpcDaGroupS = new Dictionary<string, OpcDaGroup>() };
                OpcDaServices.Add(Service);
            }

            if (Service.Service.IsConnected==false)
            {
                try
                {
                    Service.Service.Connect();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connect "+ Service.Host+ ", ServiceId "+ Service.ServiceId+"error!!"+ e.Message);
                }
                
            }

            return Service;
        }

        public IList<TreeNode> GetTreeNodes(string serviceProgId)
        {
            //var service =  _serviceCollection.Where(a => a.ServiceIds.Contains(serviceProgId))
            // .FirstOrDefault();

            var _server = GetOpcDaService(serviceProgId);

            List<TreeNode> Nodes = new List<TreeNode>();
            try
            {
                OpcDaBrowserAuto browserAuto = new OpcDaBrowserAuto(_server.Service);
                BrowseChildren(browserAuto, Nodes);
            }
            catch (Exception exp) {

                return new List<TreeNode>();
            }
            return Nodes;
        }

        private void BrowseChildren(IOpcDaBrowser browser, IList<TreeNode> Items, string itemId = null, int indent = 0)
        {
            OpcDaBrowseElement[] elements = browser.GetElements(itemId);
            foreach (OpcDaBrowseElement element in elements)
            {
                if (!(element.ItemId.IndexOf('$') == 0)) {
                    TreeNode treeNode = new TreeNode() { Name = element.ItemId, NodeType = TreeNodeType.Property };
                    Items.Add(treeNode);
                    if (element.HasChildren)
                    {
                        BrowseChildren(browser, treeNode.Children, element.ItemId, indent + 2);
                    }
                }
                
            }
        }

        bool CheckServiceExisted(ServiceCollection service, string serviceProgId) {
          return  OpcDaServices.Any(item => { return item.Host == service.Host && item.ServiceId == serviceProgId; });
        }
       
        public string StartMonitoringItems(string serviceProgId, List<string>  itemIds, string strMd5)
        {
            //var service = _serviceCollection.Where(a => a.ServiceIds.Contains(serviceProgId))
            //                  .FirstOrDefault();

            //OpcDaServer daService = null;
            //if (CheckServiceExisted(service, serviceProgId)){
            //    daService = OpcDaServices.Find(item => { return item.Host == service.Host && item.ServiceId == serviceProgId; })
            //                          .Service;
            //}
            //else {
            //    daService = new OpcDaServer(serviceProgId, service.Host);
            //    daService.Connect();
            //    OpcDaServices.Add(new OpcDaService() { Host= service.Host, ServiceId= serviceProgId, Service=daService });
            //}

            OpcDaService _server = GetOpcDaService(serviceProgId);

            string groupId = Guid.NewGuid().ToString();
            OpcDaGroup group;
            if (_server.OpcDaGroupS.ContainsKey(strMd5) == false)
            {
                //OpcDaGroup group = _server.Service.AddGroup(groupId);  // maybe cost lot of time
                group = _server.Service.AddGroup(strMd5);  // maybe cost lot of time
                group.IsActive = true;
                //_server.OpcDaGroupS.Add(groupId, group);
                _server.OpcDaGroupS.Add(strMd5, group);
            }
            else 
            {
                group = _server.OpcDaGroupS[strMd5];
            }


            List<OpcDaItemDefinition> itemDefList = new List<OpcDaItemDefinition>();
            
            itemIds.ForEach(itemId => {

                var def = new OpcDaItemDefinition();
                def.ItemId = itemId;
                def.IsActive = true;
                itemDefList.Add(def);
            });
            OpcDaItemResult[] opcDaItemResults = group.AddItems(itemDefList);
            _daGroupKeyPairs.Add(groupId, group);
            group.UpdateRate = TimeSpan.FromMilliseconds(1000);//100毫秒触发一次
            group.ValuesChanged += MonitorValuesChanged;
            return groupId;
        }

        public void StopMonitoringItems(string serviceProgId, string groupId)
        {
            var service = _serviceCollection.Where(a => a.ServiceIds.Contains(serviceProgId))
                               .FirstOrDefault();
            OpcDaServer daService = null;
            if (CheckServiceExisted(service, serviceProgId))
            {
                daService = OpcDaServices.Find(item => { return item.Host == service.Host && item.ServiceId == serviceProgId; })
                                      .Service;
                OpcDaGroup group = _daGroupKeyPairs[groupId];
                group.ValuesChanged -= MonitorValuesChanged;
                daService.RemoveGroup(group);
                _daGroupKeyPairs.Remove(groupId);
            }
        }

        public void SetItemsValueChangedCallBack(IItemsValueChangedCallBack callBack)
        {
            _callBack = callBack;
        }

        public OpcDaItemValue[] ReadItemsValues(string ServerID, List<string> Items, string GroupId, string strMd5)
        {
            OpcDaService _server = GetOpcDaService(ServerID);

            if (_server.OpcDaGroupS.ContainsKey(strMd5) == true)
            {
                OpcDaGroup group = _server.OpcDaGroupS[strMd5];
                OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);

                Console.WriteLine("ReadItemsValues " + values);

                return values;
            }

            return null;
        }


        public void WriteValues(string ServerID, string groupId, Dictionary<string,object> itemValuePairs) {

            OpcDaService _server = GetOpcDaService(ServerID);
            if (_server.OpcDaGroupS.ContainsKey(groupId) == true)
            {
                OpcDaGroup group = _server.OpcDaGroupS[groupId];

                //OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
                //Console.WriteLine("ReadItemsValues " + values);

                var keyList = itemValuePairs.Keys.ToList();
                List<OpcDaItem> itemList = new List<OpcDaItem>();
                keyList.ForEach(ids => {
                    var daItem = group.Items
                                      .Where(a => a.ItemId == ids)
                                      .FirstOrDefault();
                    itemList.Add(daItem);
                });

                object[] dd = itemValuePairs.Values.ToArray();
                HRESULT[] res = group.Write(itemList, dd);

                Console.WriteLine("Write HRESULT " + res);
            }

            //var service = _serviceCollection.Where(a => a.ServiceIds.Contains(serviceProgId))
            //                  .FirstOrDefault();
            //OpcDaServer daService = null;
            //if (CheckServiceExisted(service, serviceProgId))
            //{
            //    daService = OpcDaServices.Find(item => { return item.Host == service.Host && item.ServiceId == serviceProgId; })
            //                          .Service;
            //    if (_daGroupKeyPairs.ContainsKey(groupId)){
            //        OpcDaGroup group = _daGroupKeyPairs[groupId];
            //        var keyList = itemValuePairs.Keys.ToList();
            //        List<OpcDaItem> itemList = new List<OpcDaItem>();
            //        keyList.ForEach(ids => {
            //            var daItem = group.Items
            //                              .Where(a => a.ItemId == ids)
            //                              .FirstOrDefault();
            //            itemList.Add(daItem);
            //        });

            //        group.Write(itemList, itemValuePairs.Values.ToArray());
            //    }
               
            //}
        }

        private void MonitorValuesChanged(object sender, OpcDaItemValuesChangedEventArgs e)
        {
            if (_callBack != null)
            {
                var opcGroup = sender as OpcDaGroup;
                _callBack.ValueChangedCallBack(opcGroup.Name, e.Values);
            }
        }

    }
}
