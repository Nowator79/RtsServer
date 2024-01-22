using RtsServer.Classes.Processor;

namespace RtsServer.Classes.NetWork.Tcp
{
    public class Server : Base
    {
        public Server(int port, MainProcessor processor) : base(port, processor)
        {

        }

        public new void Run()
        {
            base.Run();
        }
    }
}
