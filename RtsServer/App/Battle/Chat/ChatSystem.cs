using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.Battle.Chat
{
    public class ChatSystem
    {
        public List<User> Users { get; private set; }
        public List<Message> Messages { get; private set; }
        private GameServer Context;

        public ChatSystem(GameServer context)
        {
            Users = [];
            Messages = [];
            Context = context;
        }

        public void AddUser(User user)
        {
            Users.Add(user);
        }

        public void RemoveUser(User user)
        {
            Users.Remove(user);
        }

        public User FindUserByUserAuth(UserAuth userAuth)
        {
           return Users.First(user => user.UserAuth == userAuth);
        }

        public void SendMessage(Message message)
        {
            Messages.Add(message);
            List<NetWork.Tcp.UserClientTcp> UsersTcp = Context.TcpServer.Users.ToList();
            foreach (NetWork.Tcp.UserClientTcp userTcp in UsersTcp)
            {
               (new SendMessageSender(userTcp)).SetDate(new NMessage(message.user.UserAuth.Id, message.message)).SendMessage();
            }
        }

        public List<Message> GetChat()
        {
            return Messages;
        }

        public void ExitUser(User user)
        {
            Users.Remove(user);
        }

        public void ExitUser(UserAuth user)
        {
            User? exitUser = Users.Find((userCur) => { return userCur.UserAuth.Id == user.Id; });
            if (exitUser != null)
            {
                ExitUser(exitUser);
            }
        }
    }
}
