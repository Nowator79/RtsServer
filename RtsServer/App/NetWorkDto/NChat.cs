using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsServer.App.NetWorkDto
{
    public class NChat
    {
        public NUser[] Users{ get; set; }
        public NMessage[] Message { get; set; }

        public NChat(NUser[] Users, NMessage[] Message)
        {
            this.Users = Users;
            this.Message = Message;
        }
    }
}
