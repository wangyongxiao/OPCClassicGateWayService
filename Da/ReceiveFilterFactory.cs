using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Net;

namespace Da
{
    public class ReceiveFilterFactory : IReceiveFilterFactory<StringRequestInfo>
    {
        //private ReceiveFilter _receiveFilter = new ReceiveFilter();

        public IReceiveFilter<StringRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, IPEndPoint remoteEndPoint)
        {
            return new ReceiveFilter();
        }
    }


    
}
