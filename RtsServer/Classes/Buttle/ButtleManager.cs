using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsServer.Classes.Buttle
{
    public class ButtleManager
    {
        public List<Game> Games { get; private set; }

        public ButtleManager()
        {
            Games = new();
        }

        public void Add(Game Game)
        {
            Games.Add(Game);
        }
        public void Clear()
        {

        }
    }
}
