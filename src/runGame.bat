csc /t:library /out:HaliteHelper.dll HaliteHelper.cs
csc /d:DEBUG /reference:HaliteHelper.dll -out:MyBot.exe MyBot.cs
csc /reference:HaliteHelper.dll -out:GenghiBot_v2.exe GenghiBot_v2.cs 
halite -d "30 30" "MyBot.exe" "GenghiBot_v2.exe"
