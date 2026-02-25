using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtsServer.App.Battle
{
    public class MatchType
    {
        public string Code { get; set; }
        public int PlayersRequired { get; set; }
        public List<string> AllowedMapCodes { get; set; } = new();
        /// <summary>Если задан и карта есть в списке — выбирается она, иначе случайная.</summary>
        public string? PreferredMapCode { get; set; }

        public MatchType(string code, int playersRequired, IEnumerable<string> mapCodes, string? preferredMapCode = null)
        {
            Code = code;
            PlayersRequired = playersRequired;
            AllowedMapCodes = new List<string>(mapCodes);
            PreferredMapCode = preferredMapCode;
        }
    }
}
