using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceInvaders;
using SpaceInvaders.Command;
using SpaceInvaders.Core;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace BasicBot
{
    public enum Players { PlayerOne=1, PlayerTwo=2};
    public class Game
    {

        Stack<Match> stack;
        
        Match Match
        {
            get
            {
                return Match.GetInstance();
            }
            set
            {
                Match.SetInstance(value);
            }
        }

        Map Map
        {
            get
            {
                return this.Match.Map;
            }
        }

        UpdateManager UpdateManager
        {
            get
            {
                return this.Match.Map.UpdateManager;
            }
        }

        public Player Player1
        {
            get { return Match.GetPlayer(1); }
        }

        public int MapWidth
        {
            get
            {
                return this.Map.Width;
            }
        }

        public int MapHeight
        {
            get
            {
                return this.Map.Height;
            }
        }
        
        public ChallengeHarnessInterfaces.MatchResult GameResult
        {
            get
            {
                return this.Match.GetResult();
            }
        }

        public bool IsGameOver
        {
            get
            {
                return this.Match.GameIsOver();
            }
        }

        public Player Player2
        {
            get { return Match.GetPlayer(2); }
        }

        public int RoundNumber
        {
            get
            {
                return Match.RoundNumber;
            }
        }

        public Game(string path)
        {
            this.stack = new Stack<Match>();
            this.Match = LoadState(path);
        }

        public Game(Match match)
        {
            this.stack = new Stack<Match>();
            this.Match = match;
        }

        public Player GetPlayer(Players player)
        {
            return this.Match.GetPlayer((int)player);
        }

        public bool IsPosClear(int x, int y)
        {
            if (this.Map.GetEntity(x, y) != null)
                return false;
            return true;
        }

        void backup_gamestate()
        {

            Match backup_state;

            backup_state = this.Match.GetFlippedCopyOfMatch().GetFlippedCopyOfMatch() as Match;
            // foreach (Player pl in this.Match.Players)
            //    pl.RespawnPlayerShipIfNecessary();
            // backup_state.Map.UpdateManager.Update();
            backup_state.Map.UpdateEntities();
            // backup_state.Map.UpdateManager.Update();

            for (int i = 1; i <= 2; i++)
            {
                var mis = this.Match.GetPlayer(i).MissileController;
                var ali = this.Match.GetPlayer(i).AlienFactory;
                var shi = this.Match.GetPlayer(i).Ship;
                if (mis != null)
                    backup_state.GetPlayer(i).MissileController = new SpaceInvaders.Entities.Buildings.MissileController(mis.Id, mis.PlayerNumber, mis.X, mis.Y, mis.Width, mis.Height, mis.Alive, mis.LivesCost);
                if (ali != null)
                    backup_state.GetPlayer(i).AlienFactory = new SpaceInvaders.Entities.Buildings.AlienFactory(ali.Id, ali.PlayerNumber, ali.X, ali.Y, ali.Width, ali.Height, ali.Alive, ali.LivesCost);
                if (shi != null)
                    backup_state.GetPlayer(i).Ship = new SpaceInvaders.Entities.Ship(shi.Id, shi.PlayerNumber, shi.X, shi.Y, shi.Width, shi.Height, shi.Alive, shi.Command, shi.CommandFeedback);

            }

            for (int i = 1; i < 3; i++)
            {
                if (this.UpdateManager.Entities[i].ContainsKey(EntityType.Ship))
                {
                    while (this.Match.Map.UpdateManager.Entities[i][EntityType.Ship].Count > 1)
                        this.UpdateManager.Entities[i][EntityType.Ship].RemoveAt(0);
                }
                else
                {
                    this.UpdateManager.Entities[i][EntityType.Ship] = new List<Entity>() { this.Match.GetPlayer(i).Ship };
                }
                this.UpdateManager.Entities[i][EntityType.Ship][0] = this.Match.GetPlayer(i).Ship;
                
            }


            backup_state.Map.UpdateManager.EntitiesUnclassified.Clear();

            stack.Push(backup_state);

        }

        void restore_gamestate()
        {
            Match = stack.Pop();
        }

        public void LogMatchState()
        {
            Log("Game state:");
            Log(String.Format("\tRound Number: {0}", this.RoundNumber));

            foreach (var player in this.Match.Players)
            {
                LogPlayerState(player);
            }
        }

        private void LogPlayerState(Player player)
        {
            Log(String.Format("\tPlayer {0} Kills: {1}", player.PlayerNumber, player.Kills));
            Log(String.Format("\tPlayer {0} Lives: {1}", player.PlayerNumber, player.Lives));
            Log(String.Format("\tPlayer {0} Missiles: {1}/{2}", player.PlayerNumber,
                player.Missiles.Count, player.MissileLimit));
        }

        public void MakeMove()
        {
            backup_gamestate();
            this.Match.Update();
        }

        public void UndoMove()
        {
            restore_gamestate();
        }

        public void SetMove(Players player, ShipCommand move)
        {
            if (player == Players.PlayerOne)
                this.Match.SetPlayerMove(1, move.ToString());
            else
                this.Match.SetPlayerMove(2, move.ToString());
        }

        public List<ShipCommand> GetPossibleMoves(Players pl)
        {
            Player player;
            if (pl == Players.PlayerOne)
                player = Player1;
            else
                player = Player2;

            int delta = 1;
            if (pl == Players.PlayerTwo)
                delta = -1;
            if (player.Ship == null)
            {
                return new List<ShipCommand>() { ShipCommand.Nothing };
            }
            if(!player.Ship.Alive)
            {
                return new List<ShipCommand>() { ShipCommand.Nothing };
            }

            List<ShipCommand> possibleMoves = new List<ShipCommand>();
            if (player.Ship.X > 1)
                possibleMoves.Add(pl == Players.PlayerOne ? ShipCommand.MoveLeft : ShipCommand.MoveRight);
            if (player.Ship.X + player.Ship.Width < MapWidth - 2)
                possibleMoves.Add(pl == Players.PlayerTwo ? ShipCommand.MoveRight : ShipCommand.MoveLeft);
            if (player.Missiles.Count < player.MissileLimit)
                possibleMoves.Add(ShipCommand.Shoot);
            if (player.Lives > 0 && IsPosClear(player.Ship.X, player.Ship.Y + 1 * delta))
            {
                if (player.MissileController == null)
                    possibleMoves.Add(ShipCommand.BuildMissileController);
                else if (!player.MissileController.Alive)
                    possibleMoves.Add(ShipCommand.BuildMissileController);
                if (player.AlienFactory == null)
                    possibleMoves.Add(ShipCommand.BuildAlienFactory);
                else if (!player.AlienFactory.Alive)
                    possibleMoves.Add(ShipCommand.BuildAlienFactory);
            }
            if (player.Lives > 0)
                possibleMoves.Add(ShipCommand.BuildShield);
            possibleMoves.Add(ShipCommand.Nothing);

            return possibleMoves;
        }

        private Match LoadState(string filename)
        {
            
            try
            {
                string jsonText;
                using (var file = new StreamReader(filename))
                {
                    jsonText = file.ReadToEnd();
                }

                return DeserializeState(jsonText);
            }
            catch (IOException e)
            {
                Log(String.Format("Unable to read state file: {0}", filename));
                var trace = new StackTrace(e);
                Log(String.Format("Stacktrace: {0}", trace));
                return null;
            }

        }
        private void Log(string message)
        {
            Console.WriteLine("[BOT]\t{0}", message);
        }
        private static Match DeserializeState(string jsonText)
        {
            var match = JsonConvert.DeserializeObject<Match>(jsonText,
                new JsonSerializerSettings
                {
                    Converters = { new EntityConverter() },
                    NullValueHandling = NullValueHandling.Ignore
                });
            for (int i = 1; i < 3; i++)
            {
                try
                {
                    while (match.Map.UpdateManager.Entities[i][EntityType.Ship].Count > 1)
                        match.Map.UpdateManager.Entities[i][EntityType.Ship].RemoveAt(0);
                    match.Map.UpdateManager.Entities[i][EntityType.Ship][0] = match.GetPlayer(i).Ship;
                }
                catch
                {
                    continue;
                }
            }

           
            match.Map.UpdateManager.EntitiesUnclassified.Clear();
            return match;
        }

        public static Players NextPlayer(Players player)
        {
            if (player == Players.PlayerOne)
                return Players.PlayerTwo;
            else
                return Players.PlayerOne;
        }
    }
}
