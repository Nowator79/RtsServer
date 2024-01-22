namespace RtsServer.Classes.Processor.Response
{
    public class BodyAuth
    {
        public string Login;
        public string Password;

        public BodyAuth(string login, string password)
        {
            Login = login;
            Password = password;
        }
    }
}
