using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIO.Emitter
{
    public class PacketObject<T>
    {
        public int type { get; set; }
        public Tuple<string, T> data { get; set; }
        public object nsp { get; set; }
    }
}
