using System;
using System.Diagnostics;
using System.IO;
using BasicBot.Properties;
using Newtonsoft.Json;
using SpaceInvaders;
using SpaceInvaders.Command;
using SpaceInvaders.Core;

namespace BasicBot
{
    public class BasicBot
    {
        public BasicBot(string outputPath)
        {
            OutputPath = outputPath;
        }

        protected string OutputPath { get; private set; }

        public void Execute()
        {
            var match = LoadState();
            LogMatchState(match);

            var map = LoadMap();
            Log(String.Format("Map:{0}{1}", Environment.NewLine, map));

            var shipCommand = ShipCommand.Nothing;
            var player1 = match.GetPlayer(1);
            var p1_ship = player1.Ship;

            
            
            //Ray trace if can shoot, no shields
            bool isClear = true;
            for (int y = match.Map.Height -8; y < match.Map.Height - 4; y++ )
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
            if (player1.Missiles.Count < player1.MissileLimit && isClear)
                shipCommand = ShipCommand.Shoot;

            //Dodge is more important
            shipCommand = DodgeBullets(match, shipCommand, p1_ship);

            if (shipCommand == ShipCommand.Nothing & player1.Lives > 0)
                shipCommand = ShipCommand.BuildMissileController;

            if (shipCommand == ShipCommand.Nothing && !isClear)
                shipCommand = ShipCommand.MoveLeft;
            else if (shipCommand == ShipCommand.Nothing && player1.Lives > 0)
                shipCommand = ShipCommand.BuildShield;
            
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