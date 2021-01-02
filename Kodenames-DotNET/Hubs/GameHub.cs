using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kodenames_DotNET.Models;
using Microsoft.AspNetCore.SignalR;
using System.Timers;


namespace Kodenames_DotNET.Hubs
{

    public class GameHub : Hub
    {
        public static List<GameSession> _sessions = new List<GameSession>();

        public static List<string> wordBank = new List<string>
        {
        "product","hall","month","poet","law","ability","blood","moment","funeral","editor","night","advice","energy","data","clothes","leader","chest","agency","skill","mud","wood","youth","studio","outcome","drawer","world","aspect","analyst","year","king","woman","airport","teacher","salad","reading","growth","food","girl","movie","two","reality","hearing","gate","power","buyer","arrival","debt","heart","area","vehicle","opinion","oven","city","variety","mood","union","finding","library","speech","goal","error","town","garbage","client","concept","son","song","map","student","worker","dealer","tea","week","sector","manager","person","lake","church","user","diamond","drawing","ear","sir","article","guest","penalty","engine","housing","drama","queen","wife","truth","fortune","loss","wedding","cousin","artisan","mom","insect","pizza","version","piano","cheek","dirt","police","hat","virus","poem","grocery","dad","tooth","steak","unit","disease","thing","family","bonus","media","way","meaning","height","beer","office","series","bath","child","success","depth","exam","thanks","speaker","tension","session","county","idea","road","control","warning","cookie","breath","nation","role","college","surgery","storage","video","mode","actor","guitar","climate","menu","nature","alcohol","flight","uncle","sack","songs","crook","flowers","hill","cattle","pollution","shelf","swim","pear","spy","smile","mind","north","dinosaurs","square","reaction","degree","eggs","pull","cobweb","hand","fireman","page","health","wire","balance","wax","trail","plantation","surprise","thread","detail","poison","nest","seed","sound","ants","toothbrush","porter","base","pail","arithmetic","shirt","horse","birds","amusement","trick","friction","team","dolls","fish","education","arm","decision","plot","battle","discussion","fact","word","tank","spring","jellyfish","color","cushion","flesh","edge","trousers","line","plate","flavor","instrument","hour","riddle","prose","wing","sugar","account","underwear","reason","end","move","cherries","work","mailbox","addition","use","dog","giraffe","shade","pump","harmony","cloth","stop","string","transport","cannon","toad","driving","skin","shock","box","tree","knife","acoustics","partner","regret","elbow","doctor","jail","lace","distance","birth","reward","shop","holiday","distribution","sidewalk","loaf","downtown","name","middle","bait","letters","glove","space","calculator","toes","slave","eye","range","believe","birthday","writing","train","marble","design","change","chicken","kick","rule","daughter","rings","room","sink","jam","truck","planes","support","dinner","start","fog","history","act","cup","frog","floor","leather","rain","scissors","suit","stocking","air","group","point","ice","match","eggnog","tendency","honey","income","whip","crack","frogs","hands","beginner","burst","wren","pizzas","shake","frame","talk","bead","trees","weather","school","dock","motion","nose","rod","stretch","baseball","grass","afternoon","heat","jar","scarecrow","twig","look","war","table","pocket","statement","cats","cap","flock","part","lettuce","board","sand"

        };
        const int ROUND_TIME = 1000;


        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Player with Id {Context.ConnectionId} Connected");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Player with Id {Context.ConnectionId} disconnected");
            GameSession roomThatPlayerLeft = _sessions.FirstOrDefault(s => s.Teams.Any(t=>t.Players.Any(p=>p.ConnectionId == Context.ConnectionId)));
            
            if (roomThatPlayerLeft != null)
            {

                LeaveRoom(Context.ConnectionId, roomThatPlayerLeft);
                Clients.Group(roomThatPlayerLeft.RoomName).SendAsync("SyncSession", roomThatPlayerLeft);
            }
            return base.OnDisconnectedAsync(exception);
        }

        /*
        * Used to handle player leaving a room
        */
        public Task LeaveRoom(string connId, GameSession gameSession)
        {
            // get the index of the team the player belongs to so we can iterate through their list and remove the player
            var indexOfTeam = gameSession.Teams.FindIndex(gs => gs.Players.Any(p => p.ConnectionId == connId));

            gameSession.Teams[indexOfTeam].Players.RemoveAll(p => p.ConnectionId == connId); // use the index to iterate through players in team and remove the one that has matching connection ID

            return Groups.RemoveFromGroupAsync(connId, gameSession.RoomName);//finally remove them from the group
        }

        /*
         * Used to handle player joining a room
         */

        public async Task PlayerJoined(Player newPlayer,int team, string roomName)
        {

            if (DoesRoomExist(roomName))
            {
                if (DoesNicknameExistsInRoom(newPlayer.Nickname, roomName))
                {
                    await Clients.Caller.SendAsync("NicknameExists", newPlayer.Nickname, roomName);
                }
                else
                {
                    var gameSession = _sessions.FirstOrDefault(s => s.RoomName == roomName);
                    await JoinRoom(newPlayer, team,roomName);

                    await Clients.Group(roomName).SendAsync("SyncSession", gameSession);
                }
            }
            else
            {

                await Clients.Caller.SendAsync("RoomInactive", roomName);
            }

           
        }

        // check to see if someone in a room already has the same nickname to prevent duplicates
        private bool DoesNicknameExistsInRoom(string nickname, string roomName)
        {
            GameSession roomGameSession = _sessions.FirstOrDefault(s => s.RoomName == roomName);

            if (roomGameSession == null)
                return false;


            // will loop through both teams and then check all players in each team to see if the nickname exists and return a bool based on that
            return roomGameSession.Teams.Any(t => t.Players.Any(p => p.Nickname == nickname));
        }

        //check to see if room exists before joining
        private bool DoesRoomExist(string roomName)
        {
            return _sessions.Any(s => s.RoomName == roomName.Trim());
        }
        
        public Task JoinRoom(Player newPlayer,int team,string roomName)
        {

            GameSession roomGameSession = _sessions.FirstOrDefault(s => s.RoomName == roomName);
            if (roomGameSession != null)
                roomGameSession.Teams[team].Players.Add(newPlayer);

            newPlayer.ConnectionId = Context.ConnectionId;
            return Groups.AddToGroupAsync(newPlayer.ConnectionId, roomName);
        }


        /*
         * Used anytime we want to change the game state and sync with all players
         */
        public Task ChangeGameState(string sessionId,int newState)
        {
            GameSession gameSession = _sessions.FirstOrDefault(sesh => sesh.SessionId.ToString() == sessionId); // get the game session to get team info and word bank

            gameSession.CurrentState = (State)newState;

            return Clients.Group(gameSession.RoomName).SendAsync("StateChanged", newState);
        }
     
        /*
         * Used whenever a word is selected and the client sends us the selection to be evaluated
         */

        public async Task BoardSelection(Move playersMove,string sessionId)
        {
            GameSession gameSession = _sessions.FirstOrDefault(sesh => sesh.SessionId.ToString() == sessionId); // get the game session to get team info and word bank

            if(gameSession != null && playersMove.SelectedBy != null) // safety check to make sure sessionId was valid and selectedBy (team index) is present
            {
                gameSession.Words.ElementAt((int)playersMove.WordSelected).Value.isPushed = true;
                MoveResult moveResult = playersMove.EvaluateMove(gameSession.Words); // get the move result

                //if they guessed their own word then reduce words remaining by 1
                if (moveResult == MoveResult.CORRECT)
                    gameSession.Teams[(int)playersMove.SelectedBy].WordsRemaining -= 1;

                else if (moveResult == MoveResult.OTHERTEAM) // if they guessed the other teams word
                {
                    // check which team made the guess and reduce the opposite team's words remaining by 1 
                    if ((int)playersMove.SelectedBy == 0)
                        gameSession.Teams[1].WordsRemaining -= 1;
                    else
                        gameSession.Teams[0].WordsRemaining -= 1;
                    
                }


                // if they selected the landmine word, then they lose and the game is over, set the winner as the opposite team
                if (moveResult == MoveResult.LANDMINE)
                {
                    gameSession.Winner = playersMove.SelectedBy == 0 ? 1 : 0;
                    gameSession.CurrentState = State.GAME_END;

                }
                if(moveResult == MoveResult.NEUTRAL)
                {
                    //TODO: after adding turns as a feature use this to end turn
                }

                //check if any of the teams have won to set the state and winner
                if (gameSession.Teams[0].WordsRemaining == 0)
                {
                    gameSession.Winner = 0;
                    gameSession.CurrentState = State.GAME_END;
                }
                if (gameSession.Teams[1].WordsRemaining == 0)
                {
                    gameSession.Winner = 1;
                    gameSession.CurrentState = State.GAME_END;
                }

                //Send message back to client once we have finished our game logic with the original player move, and the words reamining for each team
                await Clients.Group(gameSession.RoomName).SendAsync("NewMove", playersMove, gameSession.Teams[0].WordsRemaining, gameSession.Teams[1].WordsRemaining, gameSession.CurrentState);

                await Clients.Group(gameSession.RoomName).SendAsync("SyncSession", gameSession);

            }
        }

        /*
         * Used to process team changes for players
         */
        public async Task ChangeTeams(Player playerToMove, int currentTeam,int newTeam,string sessionId)
        {
            var gameSession = _sessions.FirstOrDefault(s => s.SessionId.ToString() == sessionId);

            if (playerToMove.ConnectionId == null)
                playerToMove.ConnectionId = Context.ConnectionId;

            if(gameSession != null)
            {
                gameSession.Teams[currentTeam].Players.RemoveAll(p=>p.Nickname == playerToMove.Nickname);

                playerToMove.Role = Roles.PLAYER;
                gameSession.Teams[newTeam].Players.Add(playerToMove);
            }

            await Clients.Group(gameSession.RoomName).SendAsync("SyncSession", gameSession);
        }

        /*
        * Used to process changing Roles for players
        */
        public async Task ChangeRole(Player playerToMove,int currTeam,int roleToSet, string sessionId)
        {
            var gameSession = _sessions.FirstOrDefault(s => s.SessionId.ToString() == sessionId);

            if (gameSession != null)
            {
                gameSession.Teams[currTeam].Players.First(p => p.Nickname == playerToMove.Nickname).Role = (Roles)roleToSet;
            }

            await Clients.Group(gameSession.RoomName).SendAsync("SyncSession", gameSession);
        }

        /*
        * Used to create a brand new game session given the room name and teams 
        */
        public async Task NewGame(string name, Team teamA, Team teamB,bool isTimed, int roundTime)
        {
            if (DoesRoomExist(name))
            {

                await Clients.Caller.SendAsync("RoomExists", name);
            }
            else
            {
                var random = new Random();
                List<string> randomWords = wordBank.OrderBy(w => random.Next()).Take(25).ToList();


                GameSession newGameSession = new GameSession
                {
                    SessionId = Guid.NewGuid(),
                    RoomName = name,
                    Teams = new List<Team>() { teamA, teamB },
                    CurrentState = State.GAME_PENDING,
                    GameTimer = new Timer(ROUND_TIME),
                    RoundTime = roundTime,
                    IsTimed = isTimed,
                    Words = new Dictionary<string, WordInfo>()
                };

                newGameSession.GenerateWordAndMapping(randomWords);

                _sessions.Add(newGameSession);

                await Clients.Caller.SendAsync("SessionInfo", newGameSession);
            }
           
        }

        public Task ResetGame(string sessionId)
        {
            var gameSession = _sessions.FirstOrDefault(s => s.SessionId.ToString() == sessionId); // get the current game session
            if(gameSession != null)
            {
                var random = new Random();
                List<string> randomWords = wordBank.OrderBy(w => random.Next()).Take(25).ToList();

                if(gameSession.IsTimed)
                    gameSession.GameTimer = new Timer(ROUND_TIME);

                gameSession.ResetGame(randomWords);

                return Clients.Group(gameSession.RoomName).SendAsync("SyncSession", gameSession);
            }
            else
            {
                return Clients.Caller.SendAsync("ResetError", gameSession);
            }
        }

        public void OnTimerEvent(object source, ElapsedEventArgs e)
        {
           
        }

        /*
         * Used to broadcast the time left and sync it for all clients in the game room
         */
        public Task BroadcastTimer(string roomName,int secondsLeft)
        {

            return Clients.Group(roomName).SendAsync("RoundTimer",secondsLeft);
        }


        /*
        * Used to initialize the timer and start counting down
        */
        public async Task StartTurn(string sessionId, string roomName)
        {
            var currentSession = _sessions.SingleOrDefault(sesh => sesh.RoomName == roomName && sesh.SessionId.ToString() == sessionId.Trim());

            if (currentSession == null)
                await Clients.Caller.SendAsync("GameError", "Error: The current session cannot be found");

            currentSession.GameTimer = new Timer(ROUND_TIME);
            currentSession.GameTimer.Elapsed += OnTimerEvent;
            currentSession.GameTimer.Start();

            int timeLeft = currentSession.RoundTime - currentSession.TimeElapsed;
            await BroadcastTimer(roomName, timeLeft);
        }
    }
}
