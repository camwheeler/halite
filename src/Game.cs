using System;
using System.Collections.Generic;
using System.Linq;
using static AStar;

public class Game
{
    private Map map;
    private readonly ushort myId;
    public Dictionary<Location, Unit> Units = new Dictionary<Location, Unit>();
    public Dictionary<Location, Queue<Direction>> PathTemplate = new Dictionary<Location, Queue<Direction>>();
    public Dictionary<Location, Queue<Direction>> PathDictionary = new Dictionary<Location, Queue<Direction>>();
    private List<Move> moves;
    private bool capturedProductionFacility = false;
    private Location nearestProductionFacility;

    public Game(Map map, ushort myId) {
        this.map = map;
        this.myId = myId;

        Log.Setup(@"C:\Temp\Halite.log");

        for (ushort x = 0; x < map.Width; x++)
            for (ushort y = 0; y < map.Height; y++) {
                var location = new Location(x, y);
                Units.Add(location, new Unit(map[x, y], GetNeighbors(location)));
                PathTemplate.Add(location, new Queue<Direction>());
            }
        PathDictionary = new Dictionary<Location, Queue<Direction>>(PathTemplate);

        var distance = ushort.MaxValue;
        foreach (var facility in Units.Where(u => u.Value.Site.Production == Units.Max(p => p.Value.Site.Production))) {
            var count = (ushort) MoveToLocation(Units.Single(u => u.Value.Site.Owner == this.myId).Key, facility.Key).Count;
            if (count < distance) {
                distance = count;
                nearestProductionFacility = facility.Key;
            }
        }
    }

    private List<Move> GetNeighbors(Location location) {
        var neighbors = new List<Move> {
            new Move {Direction = Direction.North, Location = new Location(location.X, (ushort) (location.Y > 0 ? location.Y - 1 : map.Height - 1))},
            new Move {Direction = Direction.South, Location = new Location(location.X, (ushort) (location.Y < map.Height - 1 ? location.Y + 1 : 0))},
            new Move {Direction = Direction.East, Location = new Location((ushort) (location.X < map.Width - 1 ? location.X + 1 : 0), location.Y)},
            new Move {Direction = Direction.West, Location = new Location((ushort) (location.X > 0 ? location.X - 1 : map.Width - 1), location.Y)}
        };
        return neighbors;
    }

    public void NextFrame() {
        Networking.getFrame(ref map);
        foreach (var key in Units.Keys.ToList()) Units[key].Site = map[key];

        var temp = new Dictionary<Location, Queue<Direction>>(PathTemplate);
        foreach (var path in PathDictionary.Where(p => p.Value.Count > 0)) {
            var direction = path.Value.Dequeue();
            if (path.Value.Count > 0)
                temp[Units[path.Key].Neighbors.Single(n => n.Direction == direction).Location] = path.Value;
        }
        PathDictionary = temp;
    }

    public void SubmitMoves() { Networking.SendMoves(moves); }

    public void SelectMoves() {
        moves = new List<Move>();
        foreach (var unit in Units.Where(u => u.Value.Site.Owner == myId)) {
            var move = new Move {Direction = Direction.Still, Location = unit.Key};
            if (PathDictionary[unit.Key].Count > 0)
                move.Direction = PathDictionary[unit.Key].Peek();

            //Open neighboring site
            else if (Units[unit.Key].Neighbors.Any(n => map[n.Location].Owner == 0)) {
                var openNeighbors = Units[unit.Key].Neighbors.Where(n => map[n.Location].Owner == 0).ToArray(); // Denotes an unoccupied site
                var weakest = openNeighbors.Last(n => map[n.Location].Strength == openNeighbors.Min(o => map[o.Location].Strength));
                if (map[weakest.Location].Strength < unit.Value.Site.Strength) move.Direction = weakest.Direction;
            }

            //Enemy neighboring site
            else if (Units[unit.Key].Neighbors.Any(n => !new[] {0, myId}.Contains(map[n.Location].Owner))) {
                var enemies = Units[unit.Key].Neighbors.Where(n => !new[] {0, myId}.Contains(map[n.Location].Owner)).ToArray(); // Denotes an enemy site
                if (enemies.Length == 1 && map[enemies.Last().Location].Strength < unit.Value.Site.Strength) move.Direction = enemies.Last().Direction;
            }

            //Neighboring sites all owned
            else if (unit.Value.Site.Strength > unit.Value.Site.Production*4 && !capturedProductionFacility) {
                if (unit.Value.Site.Owner == myId) {
                    capturedProductionFacility = true;
                    move.Direction = Direction.Still;
                }
                else {
                    var movements = MoveToLocation(unit.Key, nearestProductionFacility);
                    Log.Information($"All neighbors are mine, moving to production facility via route: {string.Join(",", movements.ToArray())}");
                    move.Direction = movements.Peek();
                    PathDictionary[unit.Key] = movements;
                }
            }

            else if (unit.Value.Site.Strength > unit.Value.Site.Production*4) {
                move.Direction = Direction.North;
            }

            moves.Add(move);
        }
    }

    public Queue<Direction> MoveToLocation(Location start, Location end) {
        //return LMovement(start, end); 
        var path = FindPath(new PathNode(start, Units), new PathNode(end, Units), (inicio, fin) => Units[fin.Location].Site.Strength, location => 0);
        var queue = new Queue<Direction>();
        using (var enumPath = path.GetEnumerator()) {
            var node = enumPath.Current;
            while (enumPath.MoveNext()) queue.Enqueue(Units[node.Location].Neighbors.Single(n => n.Location == enumPath.Current.Location).Direction);
        }
        return queue;
    }

    private Queue<Direction> LMovement(Location start, Location end) {
        var moveSet = new Queue<Direction>();
        var latitudeDelta = start.X == end.X ? 0 : Math.Abs(map.Width - end.X + start.X) < Math.Abs(end.X - start.X) ? map.Width - end.X - start.X : end.X - start.X;
        var longitudeDelta = start.Y == end.Y ? 0 : Math.Abs(map.Width - end.Y + start.Y) < Math.Abs(end.Y - start.Y) ? map.Width - end.Y - start.Y : end.Y - start.Y;

        var latMovement = latitudeDelta == 0 ? Direction.Still : latitudeDelta < 0 ? Direction.West : Direction.East;
        var longMovement = longitudeDelta == 0 ? Direction.Still : longitudeDelta < 0 ? Direction.North : Direction.South;

        for (var i = Math.Abs(latitudeDelta); i > 0; i--) moveSet.Enqueue(latMovement);

        for (var i = Math.Abs(longitudeDelta); i > 0; i--) moveSet.Enqueue(longMovement);

        return moveSet;
    }

    public class Unit
    {
        public Site Site;
        public readonly List<Move> Neighbors;

        public Unit(Site site, List<Move> neighbors) {
            Site = site;
            Neighbors = neighbors;
        }
    }

    public class PathNode : IHaveUncontestedNeighbors<PathNode>
    {
        public Location Location { get; }
        public IEnumerable<PathNode> OpenNeighbors { get; }

        public PathNode(Location location, Dictionary<Location, Unit> units) {
            Location = location;
            OpenNeighbors = units[location].Neighbors.Select(n => new PathNode(n.Location, units));
        }
    }
}