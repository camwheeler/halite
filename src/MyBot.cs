using System;

public class MyBot
{
    public const string RandomBotName = "GenghiBot";

    public static void Main(string[] args) {
        Console.SetIn(Console.In);
        Console.SetOut(Console.Out);

        ushort myID;
        var map = Networking.getInit(out myID);
        var game = new Game(map, myID);

        Networking.SendInit(RandomBotName);

        var random = new Random();
        while (true) {
            game.NextFrame();

            game.SelectMoves();

            game.SubmitMoves();
        }
    }
}