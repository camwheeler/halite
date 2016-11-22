csc /d:DEBUG /t:library /out:HaliteHelper.dll HaliteHelper.cs
csc /t:library /out:AStar.dll AStar.cs
csc /t:library /reference:HaliteHelper.dll /reference:AStar.dll /out:Game.dll Game.cs
csc /reference:HaliteHelper.dll /reference:Game.dll -out:MyBot.exe MyBot.cs
halite -d "30 30" "MyBot.exe" "GenghiBot_v2.exe"
