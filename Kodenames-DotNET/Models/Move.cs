using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kodenames_DotNET.Models
{
    public enum MoveResult
    {
        CORRECT,
        OTHERTEAM,
        NEUTRAL,
        LANDMINE,
        INVALID
    }

    //Class used to parse every 'move' made by the user, it includes the team that made the move and the index of the word they selected so we can compare and see if it was the right selection
    public class Move
    {
        public int? SelectedBy { get; set; }
        public int? WordSelected { get; set; }

   
        /*
         * Used to check and see if the selection made by team 'x' was correct
         * Parameter: The word mapping use by the game session
         * Returns: an int indicating the result type (i.e correct selection,other teams selection, neutral word, landmine)
         */
        public MoveResult EvaluateMove(IDictionary<string,WordInfo> sessionWordBank)
        {
            if (this.SelectedBy != null && this.WordSelected != null)
            {
                var wordSelected = sessionWordBank.ElementAt((int)this.WordSelected).Value;

                if ((int)wordSelected.WordType == this.SelectedBy)
                {
                    return MoveResult.CORRECT;
                }
                else
                {
                    if (wordSelected.WordType == WordTypes.NEUTRAL)
                        return MoveResult.NEUTRAL;
                    else if (wordSelected.WordType == WordTypes.LANDMINE)
                        return MoveResult.LANDMINE;
                    else
                        return MoveResult.OTHERTEAM;
                }

            }
            else
                return MoveResult.INVALID;

        }
    }
}
