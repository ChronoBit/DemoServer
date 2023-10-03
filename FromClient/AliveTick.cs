using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoServer.FromClient {
    public class AliveTick : NetUtils.ToServer.AliveTick {
        public override Task Handle() {
            (Net as NetWorker)!.LastTick = DateTime.Now;
            return Task.CompletedTask;
        }
    }
}
