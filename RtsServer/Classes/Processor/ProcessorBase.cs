
using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Response;

namespace RtsServer.Classes.Processor
{
    public class ProcessorBase : IProcessor
    {
        public virtual void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
        }
    }
}
