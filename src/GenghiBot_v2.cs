using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class MyBot
{
    public const string MyBotName = "GenghiBot_v2";

    private static List<Unit> players;
    private static Map map;
    private static ushort myID;

    public static void Main(string[] args) {
        Console.SetIn(Console.In);
        Console.SetOut(Console.Out);

        map = Networking.getInit(out myID);
#if DEBUG
        using (var writer = new StreamWriter(File.Create(@"C:\Temp\Halite.log"))) {
#endif
            /* ------
                Do more prep work, see rules for time limit
            ------ */

            Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

        var random = new Random();
            int turn = 1;
            while (true) {
#if DEBUG
                writer.WriteLine($"Starting turn {turn}");
#endif
                Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
                players = new List<Unit>();
                var moves = new List<Move>();
                for (ushort x = 0; x < map.Width; x++) {
                    for (ushort y = 0; y < map.Height; y++) {
                        players.Add(new Unit(x, y));
                    }
                }

                foreach (var player in players.Where(p => p.Site.Owner == myID)){ 
                    if (player.Site.Owner == myID) {
                        var direction = Direction.Still;
#if DEBUG
                        writer.WriteLine($"Deciding where to move for location{player}");
#endif
                        var location = new Location { X = player.X, Y = player.Y };
                        var adjacent = new List<Unit> { player.North, player.East, player.South, player.West };
#if DEBUG
                        writer.WriteLine($"\tFound adjacent players: \n\tNorth{player.North}\n\tSouth{player.South}\n\tEast{player.East}\n\tWest{player.West}");
#endif
                        var open = adjacent.Where(a => a.Site.Owner != myID);
                        if (!open.Any())
                        {
                            if (player.Site.Strength > 20) {
#if DEBUG
                                writer.WriteLine($"In the safe zone, deciding who needs help...");
#endif
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
#if DEBUG
                        writer.WriteLine($"\tMoving {direction} from [{location.X},{location.Y}]");
#endif
                        moves.Add(new Move { Direction = direction, Location = location });
                    }
                }
                Networking.SendMoves(moves); // Send moves
                turn++;
            }
#if DEBUG
        }
#endif
    }

    private static Direction FindNearestPerimeter(Unit unit)
    {
        var location = CheckBoundingSquare(unit.X, unit.Y, unit.X, unit.Y);
        var orientation = Math.Abs(unit.X - location.X) >= Math.Abs(unit.Y - location.Y) ? Orientation.Horizontal : Orientation.Vertical;
        if (orientation == Orientation.Horizontal)
            return unit.X - location.X > 0 ? Direction.West : Direction.East;
        return unit.Y - location.Y > 0 ? Direction.North : Direction.South;
    }

    private static Location CheckBoundingSquare(ushort minX, ushort minY, ushort maxX, ushort maxY)
    {
        minX = minX == 0 ? (ushort) (map.Width - 1) : (ushort) (minX - 1);
        minY = minY == 0 ? (ushort) (map.Height - 1) : (ushort) (minY - 1);
        maxX = maxX == map.Width - 1 ? (ushort) 0 : (ushort) (maxX + 1);
        maxY = maxY == map.Height - 1 ? (ushort) 0 : (ushort) (maxY + 1);

        for (int x = minX; x != maxX; Wrap(ref x, map.Width)) {
            if (map[(ushort)x, minY].Owner != myID)
                return new Location { X = (ushort)x, Y = minY };
        }

        for (int x = minX; x != maxX; Wrap(ref x, map.Width)) {
            if (map[(ushort)x, maxY].Owner != myID)
                return new Location { X = (ushort)x, Y = maxY };
        }

        for (int y = minY; y != maxY; Wrap(ref y, map.Height)) {
            if (map[minX, (ushort)y].Owner != myID)
                return new Location { X = minX, Y = (ushort)y};
        }

        for (int y = minY; y != maxY; Wrap(ref y, map.Height)) {
            if (map[maxX, (ushort)y].Owner != myID)
                return new Location { X = maxX, Y = (ushort)y };
        }

        return CheckBoundingSquare(minX, minY, maxX, maxY);
    }

    private static void Wrap(ref int i, int max)
    {
        if(i == max - 1)
            i = 0;
        else
            i++;
    }

    private class Unit
    {
        public ushort X { get; }
        public ushort Y { get; }
        public Site Site => map[X, Y];

        public Unit(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public Unit North => GetSite(Direction.North);
        public Unit East => GetSite(Direction.East);
        public Unit South => GetSite(Direction.South);
        public Unit West => GetSite(Direction.West);

        private Unit GetSite(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Unit(X, (ushort)(Y > 0 ? Y - 1 : map.Height - 1));
                case Direction.East:
                    return new Unit((ushort)(X < map.Width - 1 ? X + 1 : 0), Y);
                case Direction.South:
                    return new Unit(X, (ushort)(Y < map.Height - 1 ? Y + 1 : 0));
                case Direction.West:
                    return new Unit((ushort)(X > 0 ? X - 1 : map.Width - 1), Y);
                default:
                    return this;
            }
        }

        public override string ToString()
        {
            return $"\t[{X},{Y}]: S {Site.Strength} | O {Site.Owner} | P {Site.Production}";
        }
    }

    private enum Orientation
    {
        Horizontal,
        Vertical
    }
}
