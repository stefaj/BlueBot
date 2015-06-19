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
        Game game;
        MiniMax algo;

        public BasicBot(string outputPath)
        {
            OutputPath = outputPath;
        }
        
        protected string OutputPath { get; private set; }


        public void Execute()
        {
            var filename = Path.Combine(OutputPath, Settings.Default.StateFile);

            game = new Game(filename);
            game.LogMatchState();
            algo = new MiniMax(game);

            var map = LoadMap();
            Log(String.Format("Map:{0}{1}", Environment.NewLine, map));


            //match.Map.UpdateEntities();
            //match.Map.UpdateManager.Update();
            //match.GetPlayer(1).Ship.Update();

            //Test/
         /*   game.MakeMove();
            game.UndoMove();

            game.SetMove(Players.PlayerOne, ShipCommand.Shoot);
            game.MakeMove();
            game.UndoMove();
            */
    

            DoMove();
           

        }



        


        

        public void DoMove()
        {
            var shipCommand = ShipCommand.MoveRight;

            ScoredMove move = algo.MinMax(Players.PlayerOne);
            shipCommand = move.move;
            
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