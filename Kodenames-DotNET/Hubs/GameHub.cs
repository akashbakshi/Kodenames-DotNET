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
        "colt","engineering","magnitude","triangle","influx","bargaining","broom","chimney","chatter","gauntlet","pathologist","fort wayne","abandonment","hotel","competitor","briefcase","conjecture","glamor","grassland","procedure","fare","dot-com","aquaculture","lane","parody","deterioration","commissioner","dissection","cultivation","diameter","ambience","loner","yurt","northwest","palm","hurdle","slide","dais","keep","taboo","square","tile","performance","coyote","strainer","cotton","folly","clover","navy","leak","nay","law","diver","cog","san francisco","advancement","rigidity","molestation","outside","rabies","veranda","technicality","skating","vegetation","island","memo","egg","cuticle","editorial","dancing","crackle","grub","medium","scooter","wedlock","adjustment","maestro","pseudonym","anatomy","outback","leather","result","mist","tendril","master","trading","area","reunion","llama","wreckage","woodland","patriarch","contraption","rap","crisp","repression","crust","wake-up","pant","toenail","barbershop","shotgun","lightness","pub","luster","homeowner","apprehension","liar","photography","legend","manual","bro","hardware","imprint","californian","font","album","sieve","white","landlord","telegraph","newspaper","slope","success","collaborator","south carolina","crab","violence","nafta","reminiscence","theorist","dime","fist","droplet","affirmation","utensil","koran","raiser","association","frame","bite","cohort","consequence","axis","match","atlas","flexion","rave","violet","counterterrorism","lookout","dump","incarceration","trout","vulture","tomato","crusade","acne","outdoors","particulate","mason","hydrocarbon","aquarium","alignment","nebraska","zionist","epidemiology","instrument","karst"
        ,"corpse","dictator","pregnancy","lapse","rumbling","allergy","dashboard","supply","looter","chronicle","board","seminar","tale","seniority","temple","refuge","stink","saliva","antigen","pennant","thoroughfare","collagen","sink","newborn","annexation","signifier","jesuit","prick","scare","sill","fun","teacher-librarian","poplar","freelance","chestnut","retardation","sword","newsroom","lime","certification","lightweight","misstep","inn","wearer","grace","sit","longevity","par","goodies","colonialism","qualifier","constituent","ranch","sandpaper","curry","sagebrush","celeb","new york","wrongdoing","firewall","thing","physiology","bot","helper","switch","paranoia","brownstone","locality","punt","shout","voltage","universe","asset","starvation","mum","sheaf","markup","therapy","factory","reservist","underdog","weapon","lunatic","freezer","carpeting","masculinity","bathtub","demise","rail","prom","report","campground","spice","flake","cancer","competition","etiquette","station","pond","printing","week","civics","armoury","evil","schooling","bar","oasis","self-sufficiency","insecurity","overview","alcohol","currency","growl","ladder","competitiveness","thicket","typing","masterpiece","neuroscience","treason","bank","glassware","caper","graduation","mozzarella","program","turpentine","tester","lull","trajectory","congestion","cessation","horseman","priority","baggage","sizzle","ammo","snowball","equilibrium","abyss","munitions","vegetable","evangelical","topping","flooding","grudge","strait","autobiography","fragment","affection","infidelity","ethnographer","mainstream","maggot","fanaticism","cavern","hanging","shoreline","irritant","moon","graffiti","road","slice","touchdown","cult","bodyguard","hegemony","call-in","tie","throng","nicotine","eating","sweating","duel","globe","wicker","hop","livelihood","cod","alderman","biochemistry","brotherhood","sacrifice","construct","pepper","chateau","skyscraper","trap","blemish","t-shirt","disparity","whirlwind","tug","spoils","captive","plain","exhibition","coop","lord","resort","auspice","sauce","drawl",
        "craziness","baseball","petitioner","tram","aztec","marcher","paneling","replay","acknowledgment","thaw","edifice","polka","backdrop","renter","domino","overflow","symphony","monitoring","shorts","jackpot","meteorite","paleontologist","antecedent","scar","china","cleaning","fraternity","bind","greensboro","drift","equator","wave","outbreak","analyst","initiative","salami","predictor","guy","lifeguard","revolution","leaguer","bombardment","dismissal","deputy","summit","greatness","biography","treat","acceptability","limelight","absurdity","mexico","postmodernism","drop","neurosurgeon","housewife","paddy","smudge","fennel","defeat","basket","critic","chameleon","coursework","promise","paperback","resurrection","exchange","scoundrel","conviction","carver","pick","antipathy","alcoholism","misunderstanding","screening","dud","predictability","safari","blister","demand","bouquet","infant","omelet","turbo","east","fin","portfolio","gig","gossip","admiration","reliance","homage","slugger","storeroom","stubble","hawk","stirrup","illustration","microorganism","cruiser","listener","renewal","vice-president","brand","beating","pendant","destination","mink","offseason","holocaust","tenant","amber","cross-section","citrus","realist","respite","graduate","focus","stadium","sender","fort","gift","coaster","clown","library","kitten","airport"


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
