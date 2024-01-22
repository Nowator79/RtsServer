namespace RtsServer.App.Tools
{
    public class Handlers
    {
        private List<Action> Actions;

        public Handlers()
        {
            Actions = new List<Action>();
        }

        public void Add(Action action)
        {
            Actions.Add(action);
        }

        public void Start()
        {
            Actions.ForEach(item => item());
        }

        public void Clear()
        {
            Actions.Clear();
        }
    }
}
