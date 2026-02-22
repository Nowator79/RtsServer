using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsServer.App.Battle
{
    public class MatchType
    {
        public string Code { get; set; }               // Например: "1v1", "ffa4", "team2v2"
        public int PlayersRequired { get; set; }       // Сколько игроков нужно для запуска
        public List<string> AllowedMapCodes { get; set; } = new();

        public MatchType(string code, int playersRequired, IEnumerable<string> mapCodes)
        {
            Code = code;
            PlayersRequired = playersRequired;
            AllowedMapCodes = new List<string>(mapCodes);
        }
    }
}
