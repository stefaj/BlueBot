using SpaceInvaders.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicBot
{
    public class ScoredMove
    {
        public ScoredMove()
        {
            score = 0;
            move = ShipCommand.Nothing;
        }
        public ScoredMove(float score, ShipCommand move = ShipCommand.Nothing)
        {
            this.score = score;
            this.move = move;
        }
        public ShipCommand move;
        public float score;
    }
}
