using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MyBot
{
    public const string MyBotName = "GenghiBot";

    private static List<Unit> units;
    private static Map map;
    private static ushort myID;

    public static void Main(string[] args) {
        Console.SetIn(Console.In);
        Console.SetOut(Console.Out);

        map = Networking.getInit(out myID);
#if DEBUG
        using (var writer = new StreamWriter(File.Create(@"C:\Temp\Halite.log"))) {
#endif

#if DEBUG
            writer.WriteLine("Production map:");
#endif
            for (var x = 1; x < map.Width; x++) {
                for (var y = 1; y < map.Height; y++) {
#if DEBUG
                    writer.Write($"| {map[(ushort) x, (ushort) y].Production.ToString().PadLeft(2, ' ')} ");
#endif
                }
#if DEBUG
                writer.WriteLine("|");
#endif
            }
            /* ------
                Do more prep work, see rules for time limit
            ------ */

            Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

            var random = new Random();
            var turn = 1;
            while (true) {
#if DEBUG
                writer.WriteLine($"Starting turn {turn}");
#endif
                Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
                units = new List<Unit>();
                var moves = new List<Move>();
                for (ushort x = 0; x < map.Width; x++)
                    for (ushort y = 0; y < map.Height; y++) units.Add(new Unit(x, y));

                foreach (var unit in units.Where(p => p.Site.Owner == myID))
                    if (unit.Site.Owner == myID) {
                        var direction = Direction.Still;
#if DEBUG
                        writer.WriteLine($"Deciding where to move for location{unit}");
#endif
                        var location = new Location {X = unit.X, Y = unit.Y};
                        var adjacent = new List<Unit> {unit.North, unit.East, unit.South, unit.West};
#if DEBUG
                        writer.WriteLine(
                            $"\tFound adjacent units: \n\tNorth{unit.North}\n\tSouth{unit.South}\n\tEast{unit.East}\n\tWest{unit.West}");
#endif
                        var open = adjacent.Where(a => a.Site.Owner != myID);
                        if (!open.Any()) {
                            if (unit.Site.Strength > 20) {
#if DEBUG
                                writer.WriteLine($"In the safe zone, deciding who needs help...");
#endif
                                // Early game, we just focus on expansion
                                if (turn*2 < (map.Width + map.Height)/2)
                                    direction = FindNearestPerimeter(unit);
                                else direction = FindNearestEnemy(unit);
                            }
                        }
                        else {
                            var weakest = open.First(o => o.Site.Strength == open.Min(s => s.Site.Strength));
                            if (unit.Site.Strength > weakest.Site.Strength)
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
#if DEBUG
                        writer.WriteLine($"\tMoving {direction} from [{location.X},{location.Y}]");
#endif
                        moves.Add(new Move {Direction = direction, Location = location});
                    }
                Networking.SendMoves(moves); // Send moves
                turn++;
            }
#if DEBUG
        }
#endif
    }

    private static Direction FindNearestPerimeter(Unit unit) {
        var location = CheckBoundingSquareOwnership(unit.X, unit.Y, unit.X, unit.Y, new[] {myID});
        var orientation = Math.Abs(unit.X - location.X) >= Math.Abs(unit.Y - location.Y)
            ? Orientation.Horizontal
            : Orientation.Vertical;
        if (orientation == Orientation.Horizontal)
            return unit.X - location.X > 0 ? Direction.West : Direction.East;
        return unit.Y - location.Y > 0 ? Direction.North : Direction.South;
    }

    private static Direction FindNearestEnemy(Unit unit) {
        var location = CheckBoundingSquareOwnership(unit.X, unit.Y, unit.X, unit.Y, new ushort[] {0, myID});
        var orientation = Math.Abs(unit.X - location.X) >= Math.Abs(unit.Y - location.Y)
            ? Orientation.Horizontal
            : Orientation.Vertical;
        if (orientation == Orientation.Horizontal)
            return unit.X - location.X > 0 ? Direction.West : Direction.East;
        return unit.Y - location.Y > 0 ? Direction.North : Direction.South;
    }

    private static Location CheckBoundingSquareOwnership(ushort minX, ushort minY, ushort maxX, ushort maxY,
        ushort[] ignoredOwners) {
        minX = minX == 0 ? (ushort) (map.Width - 1) : (ushort) (minX - 1);
        minY = minY == 0 ? (ushort) (map.Height - 1) : (ushort) (minY - 1);
        maxX = maxX == map.Width - 1 ? (ushort) 0 : (ushort) (maxX + 1);
        maxY = maxY == map.Height - 1 ? (ushort) 0 : (ushort) (maxY + 1);

        for (int x = minX; x != maxX; Wrap(ref x, map.Width))
            if (!ignoredOwners.Any(o => o == map[(ushort) x, minY].Owner))
                return new Location {X = (ushort) x, Y = minY};

        for (int x = minX; x != maxX; Wrap(ref x, map.Width))
            if (!ignoredOwners.Any(o => o == map[(ushort) x, maxY].Owner))
                return new Location {X = (ushort) x, Y = maxY};

        for (int y = minY; y != maxY; Wrap(ref y, map.Height))
            if (!ignoredOwners.Any(o => o == map[minX, (ushort) y].Owner))
                return new Location {X = minX, Y = (ushort) y};

        for (int y = minY; y != maxY; Wrap(ref y, map.Height))
            if (!ignoredOwners.Any(o => o == map[maxX, (ushort) y].Owner))
                return new Location {X = maxX, Y = (ushort) y};

        return CheckBoundingSquareOwnership(minX, minY, maxX, maxY, ignoredOwners);
    }

    private static void Wrap(ref int i, int max) {
        if (i == max - 1)
            i = 0;
        else
            i++;
    }

    private class Unit
    {
        public ushort X { get; }
        public ushort Y { get; }
        public Site Site => map[X, Y];

        public Unit(ushort x, ushort y) {
            X = x;
            Y = y;
        }

        public Unit North => GetSite(Direction.North);
        public Unit East => GetSite(Direction.East);
        public Unit South => GetSite(Direction.South);
        public Unit West => GetSite(Direction.West);

        private Unit GetSite(Direction direction) {
            switch (direction) {
                case Direction.North:
                    return new Unit(X, (ushort) (Y > 0 ? Y - 1 : map.Height - 1));
                case Direction.East:
                    return new Unit((ushort) (X < map.Width - 1 ? X + 1 : 0), Y);
                case Direction.South:
                    return new Unit(X, (ushort) (Y < map.Height - 1 ? Y + 1 : 0));
                case Direction.West:
                    return new Unit((ushort) (X > 0 ? X - 1 : map.Width - 1), Y);
                default:
                    return this;
            }
        }

        public override string ToString() {
            return $"\t[{X},{Y}]: S {Site.Strength} | O {Site.Owner} | P {Site.Production}";
        }
    }

    private enum Orientation
    {
        Horizontal,
        Vertical
    }

    private enum Statistic
    {
        Strength,
        Owner,
        Production
    }
}