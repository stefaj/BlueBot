using SpaceInvaders.Command;
using SpaceInvaders.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicBot
{
    public class MiniMax
    {
        Game game;
        
        public MiniMax(Game game)
        {
            this.game = game;
        }

        float GetPlayerUtility(Players pl)
        {
            Player player = game.GetPlayer(pl);
            float utility = 0;
            utility += 5 * player.Lives;
            if (player.MissileController != null && player.MissileController.Alive)
                utility += 7;
            if (player.AlienFactory != null && player.AlienFactory.Alive)
                utility += 7;
            utility -= game.RoundNumber / 20.0f;
            utility += 2 * player.Kills;
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

        float Score(int depth, Players player)
        {
            if (game.IsGameOver)
            {
                switch (game.GameResult)
                {
                    case ChallengeHarnessInterfaces.MatchResult.PlayerOneWins:
                        return game.RoundNumber - depth;
                    case ChallengeHarnessInterfaces.MatchResult.PlayerTwoWins:
                        return -game.RoundNumber + depth;
                    case ChallengeHarnessInterfaces.MatchResult.Tie:
                        return -game.RoundNumber + depth;
                }
            }
            if (player == Players.PlayerOne)
                return GetPlayerUtility(player);
            else
                return -GetPlayerUtility(player);
        }

        /*ScoredMove Min_Max(Players player, out ShipCommand chosen_move, int depth = 0)
        {
            chosen_move = ShipCommand.Nothing;
            depth += 1;
            if (match.GameIsOver() || depth > 4)
                return Score(depth);
            List<Tuple<ShipCommand, float>> moves = new List<Tuple<ShipCommand, float>>();

            List<ShipCommand> possibleMoves = GetPossibleMoves();

            foreach (ShipCommand c in possibleMoves)
            {
                backup_gamestate();

                match.SetPlayerMove(player_turn, c.ToString());

                match.Update();

                player_turn = 2 - player_turn + 1;
                float s = Min_Max(out chosen_move, depth);
                moves.Add(new Tuple<ShipCommand, float>(c, s));
                restore_gamestate();
                player_turn = 2 - player_turn + 1;
                float curState = Score(depth);
            }
            if (player_turn == 1)
            {
                float max = float.MinValue + 1;
                ShipCommand move = ShipCommand.Nothing;
                foreach (var m in moves)
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
        */

        public ScoredMove MinMax(Players player, int depth = 0)
        {
            if(player == Players.PlayerTwo)
                depth++;
            ScoredMove best_move = new ScoredMove();
   
            
            //End state
            if(game.IsGameOver)
            {
                var res = game.GameResult;
                if (res == ChallengeHarnessInterfaces.MatchResult.PlayerOneWins)
                    return new ScoredMove(100);
                else
                    return new ScoredMove(-100);
            }
            else if (depth > 2)
            {
                float util = game.Player1.Lives - game.Player2.Lives;
                if (player == Players.PlayerOne)
                    return new ScoredMove(util);
                else
                    return new ScoredMove(-util);
            }
            //TO DO Quescient search

            if (player == Players.PlayerOne)
                best_move.score = float.MinValue + 1;
            else
                best_move.score = float.MaxValue - 1;

            var possibleMoves = game.GetPossibleMoves(player);
            foreach(var m in possibleMoves)
            {
                game.SetMove(player, m);
                if (player == Players.PlayerTwo)
                    game.MakeMove();
                var score = MinMax(Game.NextPlayer(player), depth);
                if (player == Players.PlayerTwo)
                    game.UndoMove();

                if(player == Players.PlayerOne && score.score > best_move.score)
                {
                    best_move.move = m;
                    best_move.score = score.score;
                }
                else if(player == Players.PlayerTwo && score.score < best_move.score)
                {
                    best_move.move = m;
                    best_move.score = score.score;
                }
            }
            return best_move;

        }
    }
}
