using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MyBot
{
    public const string MyBotName = "GenghiBot";

    private static List<Player> players;
    private static Map map;
    private static ushort myID;

    public static void Main(string[] args) {
        Console.SetIn(Console.In);
        Console.SetOut(Console.Out);

        map = Networking.getInit(out myID);


        /* ------
            Do more prep work, see rules for time limit
        ------ */

        Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

        var random = new Random();
        using (var writer = new StreamWriter(File.Create(@"C:\Temp\Halite.log"))) {
            int turn = 1;
            while (true) {
                writer.WriteLine($"Starting turn {turn}");
                Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
                players = new List<Player>();
                var moves = new List<Move>();
                for (ushort x = 0; x < map.Width; x++) {
                    for (ushort y = 0; y < map.Height; y++) {
                        players.Add(new Player(x, y));
                    }
                }

                foreach (var player in players.Where(p => p.Site.Owner == myID)) {
                    if (player.Site.Owner == myID) {
                        var direction = Direction.Still;
                        writer.WriteLine($"Deciding where to move for location{player}");
                        var location = new Location { X = player.X, Y = player.Y };
                        var adjacent = new List<Player> { player.North, player.East, player.South, player.West };
                        writer.WriteLine($"\tFound adjacent players: \n\tNorth{player.North}\n\tSouth{player.South}\n\tEast{player.East}\n\tWest{player.West}");
                        var open = adjacent.Where(a => a.Site.Owner != myID);
                        if (!open.Any())
                        {
                            if (player.Site.Strength > 20)
                            {
                                writer.WriteLine($"In the safe zone, deciding who needs help...");
                                direction = FindNearestPerimeter(player);
                            }
                        } else {
                            var weakest = open.First(o => o.Site.Strength == open.Min(s => s.Site.Strength));
                            if (player.Site.Strength > weakest.Site.Strength) {
                                switch (adjacent.IndexOf(weakest)) {
                                    case 0:
                                        direction = Direction.North;
                                        break;
                                    case 1:
                                        direction = Direction.East;
                                        break;
                                    case 2:
                                        direction = Direction.South;
                                        break;
                                    case 3:
                                        direction = Direction.West;
                                        break;
                                }
                            }
                        }
                        writer.WriteLine($"\tMoving {direction} from [{location.X},{location.Y}]");
                        moves.Add(new Move { Direction = direction, Location = location });
                    }
                }

                Networking.SendMoves(moves); // Send moves
                turn++;
            }
        }
    }

    private static Direction FindNearestPerimeter(Player player)
    {
        var location = CheckBoundingSquare(player.X, player.Y, player.X, player.Y);
        
    }

    private static Location CheckBoundingSquare(ushort minX, ushort minY, ushort maxX, ushort maxY)
    {
        minX = minX > 0 ? (ushort) (minX - 1) : (ushort) (map.Width - 1);
        minY = minY > 0 ? (ushort) (minY - 1) : (ushort) (map.Height - 1);
        maxX = maxX < map.Width - 1 ? (ushort) (maxX + 1) : (ushort) 0;
        maxY = maxY < map.Height - 1 ? (ushort) (maxY + 1) : (ushort) 0;

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                if (map[(ushort) x, (ushort) y].Owner != myID)
                    return new Location {X = (ushort) x, Y = (ushort) y};
            }
        }
        return CheckBoundingSquare(minX, minY, maxX, maxY);
    }

    private class Player
    {
        public ushort X { get; }
        public ushort Y { get; }
        public Site Site => map[X, Y];

        public Player(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public Player North => GetSite(Direction.North);
        public Player East => GetSite(Direction.East);
        public Player South => GetSite(Direction.South);
        public Player West => GetSite(Direction.West);

        private Player GetSite(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Player(X, (ushort)(Y > 0 ? Y - 1 : map.Height - 1));
                case Direction.East:
                    return new Player((ushort)(X < map.Width - 1 ? X + 1 : 0), Y);
                case Direction.South:
                    return new Player(X, (ushort)(Y < map.Height - 1 ? Y + 1 : 0));
                case Direction.West:
                    return new Player((ushort)(X > 0 ? X - 1 : map.Width - 1), Y);
                default:
                    return this;
            }
        }

        public override string ToString()
        {
            return $"\t[{X},{Y}]: S {Site.Strength} | O {Site.Owner} | P {Site.Production}";
        }
    }
}
