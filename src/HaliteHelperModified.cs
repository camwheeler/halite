﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Helpful for debugging.
/// </summary>
public static class Log
{
    private static string _logPath;

    /// <summary>
    /// File must exist
    /// </summary>
    public static void Setup(string logPath) {
        _logPath = logPath;
        File.Create(_logPath);
    }

    public static void MoreInformation(string message) {
#if DEBUG
        if (!string.IsNullOrEmpty(_logPath))
            File.AppendAllText(_logPath, message);
#endif
    }

    public static void Information(string message) {
#if DEBUG
        if (!string.IsNullOrEmpty(_logPath))
            File.AppendAllLines(_logPath, new[] {$"{DateTime.Now.ToShortTimeString()}: {message}"});
#endif
    }

    public static void Error(Exception exception) { Information($"ERROR: {exception.Message} {exception.StackTrace}"); }
}

public static class Networking
{
    private static string ReadNextLine() {
        var str = Console.ReadLine();
        if (str == null) throw new ApplicationException("Could not read next line from stdin");
        return str;
    }

    private static void SendString(string str) { Console.WriteLine(str); }

    /// <summary>
    /// Call once at the start of a game to load the map and player tag from the first four stdin lines.
    /// </summary>
    public static Map getInit(out ushort playerTag) {
        // Line 1: Player tag
        if (!ushort.TryParse(ReadNextLine(), out playerTag))
            throw new ApplicationException("Could not get player tag from stdin during init");

        // Lines 2-4: Map
        var map = Map.ParseMap(ReadNextLine(), ReadNextLine(), ReadNextLine());
        return map;
    }

    /// <summary>
    /// Call every frame to update the map to the next one provided by the environment.
    /// </summary>
    public static void getFrame(ref Map map) {
        map.Update(ReadNextLine());
    }

    /// <summary>
    /// Call to acknowledge the initail game map and start the game.
    /// </summary>
    public static void SendInit(string botName) {
        SendString(botName);
    }

    /// <summary>
    /// Call to send your move orders and complete your turn.
    /// </summary>
    public static void SendMoves(IEnumerable<Move> moves) {
        SendString(Move.MovesToString(moves));
    }
}

public enum Direction
{
    Still = 0,
    North = 1,
    East = 2,
    South = 3,
    West = 4
}

public class Site
{
    public Site(ushort x, ushort y, ushort width, ushort height) {
        Location = new Location {X = x, Y = y};
        North = new Location {X = x, Y = (ushort) (y > 0 ? y - 1 : height - 1)};
        South = new Location {X = x, Y = (ushort) (y < height - 1 ? y + 1 : 0)};
        East = new Location {Y = y, X = (ushort) (x < width - 1 ? x + 1 : 0)};
        West = new Location {Y = y, X = (ushort) (x > 0 ? x - 1 : width - 1)};
        Owner = 0;
        Strength = 0;
        Production = 0;
    }

    public Location Location { get; }
    public ushort Owner { get; internal set; }
    public ushort Strength { get; internal set; }
    public ushort Production { get; internal set; }

    public Location North { get; }
    public Location East { get; }
    public Location South { get; }
    public Location West { get; }

    public override string ToString() {
        return $"\t[{Location.X},{Location.Y}]: S {Strength} | O {Owner} | P {Production}";
    }

    public Location Go(Direction direction) {
        return (Location) typeof(Location).GetProperty(direction.ToString()).GetValue(this, null);
    }
}

public class Location
{
    public ushort X;
    public ushort Y;

    public override string ToString() { return $"[{X},{Y}]"; }
}

public class Move
{
    public Location Location;
    public Direction Direction;

    internal static string MovesToString(IEnumerable<Move> moves) {
        return string.Join(" ",
            moves.Select(m => string.Format("{0} {1} {2}", m.Location.X, m.Location.Y, (int) m.Direction)));
    }
}

/// <summary>
/// State of the game at every turn. Use <see cref="GetInitialMap"/> to get the map for a new game from
/// stdin, and use <see cref="NextTurn"/> to update the map after orders for a turn have been executed.
/// </summary>
public class Map
{
    public void Update(string gameMapStr) {
        var gameMapValues = new Queue<string>(gameMapStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));

        ushort x = 0, y = 0;
        while (y < Height) {
            ushort counter, owner;
            if (!ushort.TryParse(gameMapValues.Dequeue(), out counter))
                throw new ApplicationException("Could not get some counter from stdin");
            if (!ushort.TryParse(gameMapValues.Dequeue(), out owner))
                throw new ApplicationException("Could not get some owner from stdin");
            while (counter > 0) {
                _sites[x, y].Owner = owner;
                x++;
                if (x == Width) {
                    x = 0;
                    y++;
                }
                counter--;
            }
        }

        // Parse strength values
        for (y = 0; y < Height; y++)
            for (x = 0; x < Width; x++) {
                ushort strength;
                if (!ushort.TryParse(gameMapValues.Dequeue(), out strength))
                    throw new ApplicationException("Could not get some strength value from stdin");
                _sites[x, y].Strength = strength;
            }
    }

    /// <summary>
    /// Get a read-only structure representing the current state of the site at the supplied coordinates.
    /// </summary>
    public Site this[ushort x, ushort y] {
        get {
            if (x >= Width)
                throw new IndexOutOfRangeException(
                    string.Format("Cannot get site at ({0},{1}) beacuse width is only {2}", x, y, Width));
            if (y >= Height)
                throw new IndexOutOfRangeException(
                    string.Format("Cannot get site at ({0},{1}) beacuse height is only {2}", x, y, Height));
            return _sites[x, y];
        }
    }

    /// <summary>
    /// Get a read-only structure representing the current state of the site at the supplied location.
    /// </summary>
    public Site this[Location location] => this[location.X, location.Y];

    /// <summary>
    /// Returns the width of the map.
    /// </summary>
    public ushort Width => (ushort) _sites.GetLength(0);

    /// <summary>
    ///  Returns the height of the map.
    /// </summary>
    public ushort Height => (ushort) _sites.GetLength(1);

    #region Implementation

    private readonly Site[,] _sites;

    private Map(ushort width, ushort height) {
        _sites = new Site[width, height];
        for (ushort x = 0; x < width; x++)
            for (ushort y = 0; y < height; y++) _sites[x, y] = new Site(x, y, width, height);
    }

    private static Tuple<ushort, ushort> ParseMapSize(string mapSizeStr) {
        ushort width, height;
        var parts = mapSizeStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !ushort.TryParse(parts[0], out width) || !ushort.TryParse(parts[1], out height))
            throw new ApplicationException("Could not get map size from stdin during init");
        return Tuple.Create(width, height);
    }

    public static Map ParseMap(string mapSizeStr, string productionMapStr, string gameMapStr) {
        var mapSize = ParseMapSize(mapSizeStr);
        var map = new Map(mapSize.Item1, mapSize.Item2);

        var productionValues =
            new Queue<string>(productionMapStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));

        ushort x, y;
        for (y = 0; y < map.Height; y++)
            for (x = 0; x < map.Width; x++) {
                ushort production;
                if (!ushort.TryParse(productionValues.Dequeue(), out production))
                    throw new ApplicationException("Could not get some production value from stdin");
                map._sites[x, y].Production = production;
            }

        map.Update(gameMapStr);

        return map;
    }

    public List<Site> GetOwnedSites(ushort[] ownedBy) {
        var ownedSites = new List<Site>();
        for (ushort y = 0; y < Height; y++)
            for (ushort x = 0; x < Width; x++)
                if (ownedBy.Contains(_sites[x, y].Owner))
                    ownedSites.Add(_sites[x, y]);
        return ownedSites;
    }

    public Direction FindNearestPerimeter(Site site, ushort myId) {
        var location = CheckBoundingSquareOwnership(site.Location, site.Location, new[] {myId});
        var orientation = Math.Abs(site.Location.X - location.X) > Math.Abs(site.Location.Y - location.Y)
            ? Orientation.Horizontal
            : Orientation.Vertical;
        if (orientation == Orientation.Horizontal)
            return site.Location.X - location.X > 0 ? Direction.West : Direction.East;
        return site.Location.Y - location.Y > 0 ? Direction.North : Direction.South;
    }

    private Location CheckBoundingSquareOwnership(Location min, Location max, ushort[] ignoredOwners) {
        min = this[this[min].West].North;
        max = this[this[min].East].South;

        for (var x = min.X; x != max.X; x = this[x, min.Y].East.X)
            if (!ignoredOwners.Any(o => o == this[x, min.Y].Owner))
                return new Location {X = x, Y = min.Y};

        for (var x = min.X; x != max.X; x = this[x, max.Y].East.X)
            if (!ignoredOwners.Any(o => o == this[x, max.Y].Owner))
                return new Location {X = x, Y = max.Y};

        for (var y = min.Y; y != max.Y; y = this[min.X, y].South.Y)
            if (!ignoredOwners.Any(o => o == this[min.X, y].Owner))
                return new Location {X = min.X, Y = y};

        for (var y = min.Y; y != max.Y; y = this[max.X, y].South.Y)
            if (!ignoredOwners.Any(o => o == this[max.X, y].Owner))
                return new Location {X = max.X, Y = y};
        return CheckBoundingSquareOwnership(min, max, ignoredOwners);
    }

    #endregion
}

public enum Orientation
{
    Horizontal,
    Vertical
}