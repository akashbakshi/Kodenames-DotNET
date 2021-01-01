using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kodenames_DotNET.Models
{
    public class Board
    {
        public List<int> TeamAWords { get; set; }
        public List<int> TeamBWords { get; set; }

        public List<int> NeutralWords { get; set; }

        public int LandmineWord { get; set; }
    }
}
