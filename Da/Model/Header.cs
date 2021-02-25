using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Da
{
    public struct Header
    {
        public int Id;
        public int Cmd;
        public int ErrorCode;
        public int ContentSize;

        public Header(int id, int cmd, int errorcode, int payloadLength)
        {
            Id = id;
            Cmd = cmd;
            ErrorCode = errorcode;
            ContentSize = payloadLength;
        }
    }
}
