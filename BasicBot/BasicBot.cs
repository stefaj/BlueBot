using System;
using System.Diagnostics;
using System.IO;
using BasicBot.Properties;
using Newtonsoft.Json;
using SpaceInvaders;
using SpaceInvaders.Command;
using SpaceInvaders.Core;
using System.Collections.Generic;

namespace BasicBot
{
    public class BasicBot
    {

        
        Stack<Match> match_stack;
        
        Player Player1
        {
            get { return match.GetPlayer(1); }
        }
        Player Player2
        {
            get { return match.GetPlayer(2); }
        }
        Match match
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

        public BasicBot(string outputPath)
        {
            OutputPath = outputPath;
        }
        
        protected string OutputPath { get; private set; }

        int player_turn;

        void backup_gamestate()
        {
            
            Match backup_state;

            backup_state = match.GetFlippedCopyOfMatch().GetFlippedCopyOfMatch() as Match;
            foreach (Player pl in match.Players)
                pl.RespawnPlayerShipIfNecessary();
            backup_state.Map.UpdateManager.Update(); 
          //  backup_state.Map.UpdateEntities();
           // backup_state.Map.UpdateManager.Update();

            for (int i = 1; i <= 2; i++)
            {
                var mis = match.GetPlayer(i).MissileController;
                var ali = match.GetPlayer(i).AlienFactory;
                var shi = match.GetPlayer(i).Ship;
                if (mis != null)
                    backup_state.GetPlayer(i).MissileController = new SpaceInvaders.Entities.Buildings.MissileController(mis.Id, mis.PlayerNumber, mis.X, mis.Y, mis.Width, mis.Height, mis.Alive, mis.LivesCost);
                if(ali != null)
                    backup_state.GetPlayer(i).AlienFactory = new SpaceInvaders.Entities.Buildings.AlienFactory(ali.Id, ali.PlayerNumber, ali.X, ali.Y, ali.Width, ali.Height, ali.Alive, ali.LivesCost);
                if(shi != null)
                    backup_state.GetPlayer(i).Ship = new SpaceInvaders.Entities.Ship(shi.Id, shi.PlayerNumber, shi.X, shi.Y, shi.Width, shi.Height, shi.Alive, shi.Command, shi.CommandFeedback);
                
            }

            try
            {
                for (int i = 1; i < 3; i++)
                    while (match.Map.UpdateManager.Entities[i][EntityType.Ship].Count > 1)
                        match.Map.UpdateManager.Entities[i][EntityType.Ship].RemoveAt(0);
            }
            catch
            { }
            try
            {
                match.Map.UpdateManager.Entities[1][EntityType.Ship][0] = match.GetPlayer(1).Ship;
            }
            catch
            {

                match.Map.UpdateManager.Entities[1][EntityType.Ship] = new List<Entity>();
                match.Map.UpdateManager.Entities[1][EntityType.Ship].Add(match.GetPlayer(1).Ship);
            }
            try
            {
                match.Map.UpdateManager.Entities[2][EntityType.Ship][0] = match.GetPlayer(2).Ship;
            }
            catch
            {
                match.Map.UpdateManager.Entities[2][EntityType.Ship] = new List<Entity>();
                match.Map.UpdateManager.Entities[2][EntityType.Ship].Add(match.GetPlayer(2).Ship);
            }
        
            backup_state.Map.UpdateManager.EntitiesUnclassified.Clear();
            
            match_stack.Push(backup_state);

        }

        void restore_gamestate()
        {
            match = match_stack.Pop();
        }

        public void Execute()
        {
            match_stack = new Stack<Match>();

            match = LoadState();
            match.Map.UpdateManager.Update();
            backup_gamestate();
            restore_gamestate();
           
            
            LogMatchState(match);

            var map = LoadMap();
            Log(String.Format("Map:{0}{1}", Environment.NewLine, map));
            player_turn = 1;


            //match.Map.UpdateEntities();
            //match.Map.UpdateManager.Update();
            //match.GetPlayer(1).Ship.Update();

            //Test/
        /*    float baseScore = Score(0);
            backup_gamestate();
            match.SetPlayerMove(1, ShipCommand.BuildAlienFactory.ToString());
            match.Update();
            float alienScore = Score(0);
            restore_gamestate();

            backup_gamestate();
            match.SetPlayerMove(1, ShipCommand.MoveRight.ToString());
            match.Update();
            float moveRighTScore = Score(0);
            restore_gamestate();

            backup_gamestate();
            match.SetPlayerMove(1, ShipCommand.Shoot.ToString());
            match.Update();
            float shootScore = Score(0);
            restore_gamestate();

            return;*/
            DoMove();
           

        }

        float Score(int depth)
        {
            if(match.GameIsOver())
            {
                switch (match.GetResult())
                {
                    case ChallengeHarnessInterfaces.MatchResult.PlayerOneWins:
                        return match.RoundNumber-depth;
                    case ChallengeHarnessInterfaces.MatchResult.PlayerTwoWins:
                        return -match.RoundNumber +depth;
                    case ChallengeHarnessInterfaces.MatchResult.Tie:
                        return -match.RoundNumber +depth;
                }
            }
            if (player_turn == 1)
                return GetPlayerUtility(1);
            else
                return  -GetPlayerUtility(2);
        }

        float GetPlayerUtility(int playerNumber)
        {
            Player player = match.GetPlayer(playerNumber);
            float utility = 0;
            utility += 5*player.Lives;
            if (player.MissileController != null && player.MissileController.Alive)
                utility += 7;
            if (player.AlienFactory != null && player.AlienFactory.Alive)
                utility += 7;
            utility -= match.RoundNumber / 20.0f;
            utility += 2*player.Kills;
            utility -= player.AlienWaveSize;
            utility += player.Missiles.Count;
            utility += player.MissileLimit;
            /*
            for (int x = 1; x < match.Map.Width - 1; x++ )
            {
                if (playerNumber == 1)
                    for (int y = match.Map.Height / 2; y < match.Map.Height - 2; y++)
                    {
                        var entity = match.Map.GetEntity(x, y);
                        if(entity != null)
                        {
                            if (entity.Type == EntityType.Ship)
                                utility += 1;
                        }
                    }
                else
                    for (int y = match.Map.Height / 2; y > 1; y--)
                    {
                        var entity = match.Map.GetEntity(x, y);
                        if (entity != null)
                        {
                            if (entity.Type == EntityType.Ship)
                                utility += 1;
                        }
                    }
            }*/
                    

                    //MatchRunner r;     
                    return utility;
        }

        float Min_Max(out ShipCommand chosen_move, int depth=0)
        {
            chosen_move = ShipCommand.Nothing;
            depth += 1;
            if (match.GameIsOver() || depth > 4)
                return Score(depth);
            List<Tuple<ShipCommand, float>> moves = new List<Tuple<ShipCommand, float>>();

            List<ShipCommand> possibleMoves = GetPossibleMoves();

            foreach(ShipCommand c in possibleMoves)
            {
                backup_gamestate();
                
                match.SetPlayerMove(player_turn, c.ToString());

                match.Update();

                player_turn = 2 - player_turn + 1;
                float s = Min_Max(out chosen_move, depth);
                moves.Add(new Tuple<ShipCommand,float>(c,s));
                restore_gamestate();
                player_turn = 2 - player_turn + 1;
                float curState = Score(depth);
            }
            if(player_turn == 1)
            {
                float max = float.MinValue + 1;
                ShipCommand move = ShipCommand.Nothing;
                foreach(var m in moves)
                {
                    if (m.Item2 > max)
                    {
                        move = m.Item1;
                        max = m.Item2;
                    }
                }
                chosen_move = move;
                return max;

            }
            else
            {
                float min = float.MaxValue - 1;
                ShipCommand move = ShipCommand.Nothing;
                foreach (var m in moves)
                {
                    if (m.Item2 < min)
                    {
                        move = m.Item1;
                        min = m.Item2;
                    }
                }
                chosen_move = move;
                return min;
            }            
        }

        private List<ShipCommand> GetPossibleMoves()
        {
            Player player;
            Match m;
            if (player_turn == 1)
            {
                m = match;
                player = Player1;
            }
            else
            {
                m = match.GetFlippedCopyOfMatch() as Match;
                player = m.GetPlayer(1);

                var flippedEntities = new Dictionary<int, Entity>();
                var flipper = new CoordinateFlipper(match.Map.Width, match.Map.Height);


                if (match.GetPlayer(2).MissileController != null)
                    player.MissileController = SpaceInvaders.Entities.Buildings.MissileController.CopyAndFlip(match.GetPlayer(2).MissileController, flipper, flippedEntities);
                if(match.GetPlayer(2).AlienFactory != null)
                    player.AlienFactory = SpaceInvaders.Entities.Buildings.AlienFactory.CopyAndFlip(match.GetPlayer(2).AlienFactory, flipper, flippedEntities);
                if (match.GetPlayer(2).Ship != null)
                    player.Ship = SpaceInvaders.Entities.Ship.CopyAndFlip(match.GetPlayer(2).Ship, flipper, flippedEntities);
                
            }


            List<ShipCommand> possibleMoves = new List<ShipCommand>();
            if (player.Ship.X > 1)
                possibleMoves.Add(ShipCommand.MoveLeft);
            if (player.Ship.X + player.Ship.Width < m.Map.Width - 2)
                possibleMoves.Add(ShipCommand.MoveRight);
            if (player.Missiles.Count < player.MissileLimit)
                possibleMoves.Add(ShipCommand.Shoot);
            if (player.Lives > 0 && m.Map.GetEntity(player.Ship.X, player.Ship.Y - 1) == null)
            {
                if (player.MissileController == null)
                    possibleMoves.Add(ShipCommand.BuildMissileController);
                else if(!player.MissileController.Alive)
                    possibleMoves.Add(ShipCommand.BuildMissileController);
                if (player.AlienFactory == null)
                    possibleMoves.Add(ShipCommand.BuildAlienFactory);
                else if(!player.AlienFactory.Alive)
                    possibleMoves.Add(ShipCommand.BuildAlienFactory);
            }
            if (player.Lives > 0)
                possibleMoves.Add(ShipCommand.BuildShield);
            possibleMoves.Add(ShipCommand.Nothing);

            return possibleMoves;
        }

        public void DoMove()
        {
            var shipCommand = ShipCommand.MoveRight;
            var p1_ship = Player1.Ship;

            float s = Min_Max(out shipCommand);
            
            
            //Ray trace if can shoot, no shields
         /*   bool isClear = true;
            for (int y = match.Map.Height - 8; y < match.Map.Height - 4; y++)
            {
                Entity en = match.Map.GetEntity(p1_ship.X + 1, y);
                if (en == null)
                    continue;
                if (en.Type == EntityType.Shield)
                {
                    isClear = false;
                    break;
                }

            }
            if (Player1.Missiles.Count < Player1.MissileLimit && isClear)
                shipCommand = ShipCommand.Shoot;

            //Dodge is more important
            shipCommand = DodgeBullets(match, shipCommand, p1_ship);*/

            SaveShipCommand(shipCommand);
            match.Update();
        }



        private static ShipCommand DodgeBullets(Match match, ShipCommand shipCommand, SpaceInvaders.Entities.Ship p1_ship)
        {
            for (int y = match.Map.Height - 6; y < match.Map.Height - 1; y++)
            {
                for (int x = 1; x < match.Map.Width; x++)
                {

                    Entity en = match.Map.GetEntity(x, y);
                    if (en == null)
                        continue;
                    if (en.Type == EntityType.Bullet)
                    {
                        //Will hit, move out of the way
                        if (en.X <= p1_ship.X + p1_ship.Width - 1 && en.X >= p1_ship.X)
                        {
                            int dx = en.X - (p1_ship.X + 1);
                            if (dx > 0)
                                shipCommand = ShipCommand.MoveLeft;
                            else
                                shipCommand = ShipCommand.MoveRight;
                        }
                    }
                }
            }
            return shipCommand;
        }
        bool Collide(Entity a, Entity b)
        {
            return !
                (b.X > a.X + a.Width
                || b.X + b.Width < a.X
                || b.Y > a.Y + a.Height
                || b.Y + b.Height < a.Y);
        }

        private Match LoadState()
        {
            var filename = Path.Combine(OutputPath, Settings.Default.StateFile);
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

        private static Match DeserializeState(string jsonText)
        {
            var match = JsonConvert.DeserializeObject<Match>(jsonText,
                new JsonSerializerSettings
                {
                    Converters = {new EntityConverter()},
                    NullValueHandling = NullValueHandling.Ignore
                });
            for (int i = 1; i < 3; i++ )
                while (match.Map.UpdateManager.Entities[i][EntityType.Ship].Count > 1)
                    match.Map.UpdateManager.Entities[i][EntityType.Ship].RemoveAt(0);
            match.Map.UpdateManager.Entities[1][EntityType.Ship][0] = match.GetPlayer(1).Ship;
            match.Map.UpdateManager.Entities[2][EntityType.Ship][0] = match.GetPlayer(2).Ship;
            match.Map.UpdateManager.EntitiesUnclassified.Clear();
            return match;
        }

        private void LogMatchState(Match match)
        {
            Log("Game state:");
            Log(String.Format("\tRound Number: {0}", match.RoundNumber));
            
            foreach (var player in match.Players)
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

        private string LoadMap()
        {
            var filename = Path.Combine(OutputPath, Settings.Default.MapFile);
            try
            {
                using (var file = new StreamReader(filename))
                {
                    return file.ReadToEnd();
                }
            }
            catch (IOException e)
            {
                Log(String.Format("Unable to read map file: {0}", filename));
                var trace = new StackTrace(e);
                Log(String.Format("Stacktrace: {0}", trace));
                return "Failed to load map!";
            }
        }

        private ShipCommand GetRandomShipCommand()
        {
            var random = new Random();
            var possibleShipCommands = Enum.GetValues(typeof (ShipCommand));
            return (ShipCommand) possibleShipCommands.GetValue(random.Next(0, possibleShipCommands.Length));
        }

        private void SaveShipCommand(ShipCommand shipCommand)
        {
            var shipCommandString = shipCommand.ToString();
            var filename = Path.Combine(OutputPath, Settings.Default.OutputFile);
            try
            {
                using (var file = new StreamWriter(filename))
                {
                    file.WriteLine(shipCommandString);
                }

                Log("Command: " + shipCommandString);
            }
            catch (IOException e)
            {
                Log(String.Format("Unable to write command file: {0}", filename));

                var trace = new StackTrace(e);
                Log(String.Format("Stacktrace: {0}", trace));
            }
        }

        private void Log(string message)
        {
            Console.WriteLine("[BOT]\t{0}", message);
        }
    }
}