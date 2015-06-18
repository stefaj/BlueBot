using SpaceInvaders.Core;
using SpaceInvaders.Entities;
using SpaceInvaders.Entities.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicBot
{
    public static class Extensions
    {
        public static Bullet Copy(this Bullet bullet)
        {
            return new Bullet(bullet);
        }

        public static Alien Copy(this Alien alien)
        {
            return new Alien(alien);
        }

        public static Missile Copy(this Missile missile)
        {
            return new Missile(missile);
        }

        public static Shield Copy(this Shield shield)
        {
            return new Shield(shield);
        }

        public static Ship Copy(this Ship ship)
        {
            return new Ship(ship);
        }

        public static Wall Copy(this Wall wall)
        {
            return new Wall(wall);
        }

        public static AlienFactory Copy(this AlienFactory alienfac)
        {
            return new AlienFactory(alienfac);
        }

        public static MissileController Copy(this MissileController controller)
        {
            return new MissileController(controller);
        }

        public static Player Copy(this Player player)
        {
            return new Player(player);
        }

        public static Map Copy(this Map map)
        {
            var copy = new Map(map);

            // Copy all entities including walls
            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    var entity = map.GetEntity(x, y);

                    if (entity == null) continue;

                    Entity copiedEntity = null;
                    if (entity.GetType() == typeof(Alien))
                    {
                        copiedEntity = Copy(entity as Alien);
                    }
                    else if (entity.GetType() == typeof(Missile))
                    {
                        copiedEntity = Copy(entity as Missile);
                    }
                    else if (entity.GetType() == typeof(Bullet))
                    {
                        copiedEntity = Copy(entity as Bullet);
                    }
                    else if (entity.GetType() == typeof(Shield))
                    {
                        copiedEntity = Copy(entity as Shield);
                    }
                    else if (entity.GetType() == typeof(Ship))
                    {
                        copiedEntity = Copy(entity as Ship);
                    }
                    else if (entity.GetType() == typeof(AlienFactory))
                    {
                        copiedEntity = Copy(entity as AlienFactory);
                    }
                    else if (entity.GetType() == typeof(MissileController))
                    {
                        copiedEntity = Copy(entity as MissileController);
                    }
                    else if (entity.GetType() == typeof(Wall))
                    {
                        copiedEntity = Copy(entity as Wall);
                    }

                    if (copiedEntity != null)
                    {
                        copy.AddEntity(copiedEntity);
                    }
                }
            }
            copy.UpdateManager.AddNewEntities();
            copy.UpdateManager.RemoveKilledEntities();

            return copy;
        }

        public static Match Copy(this Match match)
        {
            var copy = new Match()
            {
                BuildingsAvailable = match.BuildingsAvailable,
                RoundLimit = match.RoundLimit,
                RoundNumber = match.RoundNumber
            };

            copy.Players.Clear();
            copy.Map = match.Map.Copy();
            copy.Players.Add(match.GetPlayer(2).Copy());
            copy.Players.Add(match.GetPlayer(1).Copy());

            return copy;
        }
    }
}
