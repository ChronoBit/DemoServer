using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetUtils;

namespace DemoServer {
    public class NetWorker : NetControl {
        public DateTime LastTick = DateTime.Now;
        public bool HasAlive => (DateTime.Now - LastTick).TotalSeconds < 15;

        public NetWorker(NetworkStream stream) : base(stream) { }
    }
}
