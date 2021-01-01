using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;

namespace Kodenames_DotNET.Models
{
    // Use to track states during the game
    public enum State
    {
        GAME_PENDING,
        GAME_START,
        ROUND_INPROGRESS,
        ROUNG_END,
        GAME_END
    }

    public enum WordTypes
    {
        TEAMA,
        TEAMB,
        NEUTRAL,
        LANDMINE
    }
    public class GameSession
    {
        public Guid SessionId { get; set; }

        public State CurrentState { get; set; }

        public string RoomName { get; set; }

        public bool IsTimed { get; set; }

        public int RoundTime { get; set; }

        public int TimeElapsed { get; set; }

        [JsonIgnore]
        public Timer GameTimer { get; set; }

        public List<Team> Teams { get; set; }

        public Dictionary<string,WordInfo> Words { get; set; }

        public int? Winner { get; set; }

        /*
         * This will take words from our wordbank and split them into their respective groups(Team A, Team B,Neutral and Landmine)
         * It will then add it to our dictionary and shuffle them in a random order
         */
        public void GenerateWordAndMapping(List<string> availableWords)
        {
            const int NeutralNum = 6;

            var random = new Random();

            var TeamAWords = availableWords.OrderBy(w => random.Next()).Take(this.Teams[0].WordsRemaining).ToList();
            availableWords.RemoveAll(TeamAWords.Contains); // remove words we just assigned to teamA from the list of available words

            var TeamBWords = availableWords.OrderBy(w => random.Next()).Take(this.Teams[1].WordsRemaining).ToList(); // pick random words and take the words remaining amount
            availableWords.RemoveAll(TeamBWords.Contains); // remove words we just assigned to TeamB

            var NeutralWords = availableWords.OrderBy(w => random.Next()).Take(NeutralNum).ToList();
            availableWords.RemoveAll(NeutralWords.Contains); // remove words we just assigned to TeamB

            var LandmineWord = availableWords.First();
            availableWords.Remove(LandmineWord); // remove words we just assigned to TeamB

            var tmpDictionary = new Dictionary<string, WordInfo>();
            // Add all the team a words with the teamA tag
            foreach(var word in TeamAWords)
            {

                tmpDictionary.Add(word, new WordInfo { WordType = WordTypes.TEAMA,isPushed = false });
            }

            // Add all the team a words with the teamB tag
            foreach (var word in TeamBWords)
            {

                tmpDictionary.Add(word, new WordInfo { WordType = WordTypes.TEAMB, isPushed = false });
            }

            // Add all the team a words with the neutral tag
            foreach (var word in NeutralWords)
            {

                tmpDictionary.Add(word, new WordInfo { WordType = WordTypes.NEUTRAL, isPushed = false });
            }

            // Add all the team a words with the landmine tag
            tmpDictionary.Add(LandmineWord, new WordInfo { WordType = WordTypes.LANDMINE, isPushed = false });


            //Shuffle words so they're in a random order
            this.Words = tmpDictionary.OrderBy(w => random.Next()).ToDictionary(item=>item.Key,item=>item.Value);
        }

        public void ResetGame(List<string> newWordBank)
        {
            this.CurrentState = State.GAME_PENDING;
            this.Winner = null;
            foreach(var player in this.Teams[0].Players.Where(p=>p.Role == Roles.SPYMASTER))
            {
                player.Role = Roles.PLAYER;
            }
            foreach (var player in this.Teams[1].Players.Where(p => p.Role == Roles.SPYMASTER))
            {
                player.Role = Roles.PLAYER;
            }
            this.CoinFlipForStartingTeam();
            this.GenerateWordAndMapping(newWordBank);
        }

        public void CoinFlipForStartingTeam()
        {
            var random = new Random();
            int teamToStart = random.Next(0, 2); // will get a random number between 0 and 1 to decide which team starts with 9 words

            // check to see if teamToStart matches the team index and set 9 or 8 based on that
            this.Teams[0].WordsRemaining = teamToStart == 0 ? 9 : 8;
            this.Teams[1].WordsRemaining = teamToStart == 1 ? 9 : 8;


        }
    }
}
