using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Program;

class Program
{
    static volatile bool aksitolt = false; // volatile az azért felelős, hogy mindig a legfrissebb értékünk legyen, és ne egy korábban eltárolt érték az aksitolt kapcsán
    static void Main()
    {
        int megadottszam = 0;
        bool indithato = false;

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("===== <> MARS ROVER RENDSZER INDÍTÁSA <> =====");
        Console.ResetColor();

        
        while (indithato == false)
        {
            Console.Write("Kérlek adj meg egy számot az indításhoz (minimum 24, óra): ");
            string bevitel = Console.ReadLine();

            // Megnézzük, hogy egyáltalán számot írt-e be a felhasználó
            if(int.TryParse(bevitel, out megadottszam))
            {
                if (megadottszam >= 24)
                {
                    indithato = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"A(z) {megadottszam} megfelelő. A szimuláció indul...");
                    Console.ResetColor();
                    Thread.Sleep(3000);
                    Console.Clear(); // 3 másodperc múlva eltűnik az egész képernyő, és indul a program
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Hiba: A(z) {megadottszam} túl kicsi! Legalább 24 kell.");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor= ConsoleColor.Red;
                Console.WriteLine("Hiba: Kérlek, érvényes számot adj meg!");
                Console.ResetColor();
            }
        }
        //Console.WriteLine("Hello world!");
        string[] lines = File.ReadAllLines("mars_map_50x50.csv"); // Beolvasom a fájlt



        // Csinálok egy 2D-s grid-et magassággal és hosszal. A vessző a zárójelben adja meg, hogy 2D-s grid-ről beszélünk.

        char[,] grid = LoadGrid(lines);


        // Egy nap számolásához szükéges elemek és a kiírott szövegek elhelyezése

        int day = 0;
        bool night = true;

        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        int infoResz = height;
        int infoCel = height + 1;
        int infoTime = height + 2;
        int infoAksi = height + 3;
        int infoLocation = height + 4;
        int infoSebesseg = height + 5;
        int infoPause = height + 6;

        // Egy új Thread-et, azaz mini programot hozok létre, ami függetlenül működik a Main függvényben lévő fő pathfinding programtól.
        // Ez azért kell, mert a nappalok és az éjszakák váltása a végtelenségig megy, ami akadályozná a pathfinding működését.
        // A pathfinding ideiglenes, a nappalok és éjszakák váltása örökké tart, és a program sorról sorra megy, ezért kell külön venni ezeket.
        // Az új Thread-be rakom bele a nappalok és az éjszakák változásának kódját. Ezt jelenti a (() => {...}) rész
        Thread thread = new Thread(() =>
        {
            while (true)
            {
                day += 1;
                System.Threading.Thread.Sleep(1000);
                if (day >= 64 && day <= 96)
                {
                    if (!night)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.SetCursorPosition(0, infoTime); // A villogó kis téglalapot elállítom, ahol az infoTime van az Y tengelyen, azaz a mapom alá 2 sorral
                        Console.Write("Éjszaka van    "); // 64 másodperc = 16 óra és 96 másodperc = 24 óra
                        // Azért raktam jó pár Space-t a kiírás után, mert teszt során láttam, hogy az Éjszaka felirat hosszabb, mint a Nappal felirat.
                        // Tehát valami az Éjszakából hátramaradt a Nappalra, azaz 'Nappal vann' volt a kiírás, amit így javítottam ki.
                        Console.ResetColor();
                        aksitolt = false; // Ne töltsön az aksi este
                    }
                    night = true;
                }
                else if (day >= 0 && day <= 64)
                {
                    if (night)
                    {
                        Console.ForegroundColor= ConsoleColor.Cyan;
                        Console.SetCursorPosition(0, infoTime); // A villogó kis téglalapot elállítom, ahol az infoTime van az Y tengelyen, azaz a mapom alá 2 sorral
                        Console.Write("Nappal van    "); // 0 másodperc = hajnal/0 óra és 64 másodperc = 16 óra
                        Console.ResetColor();
                        aksitolt = true; // Töltsön az aksi nappal
                    }
                    night = false;
                }
                if (day == 96)
                {
                    day = 0; // Ha az óra eléri a 96 másodpercet, tehát a 24 órát, akkor visszaáll az óra 0-ra
                }
            }
        });
        thread.IsBackground = true; // A Thread, azaz a külön program működik és nem áll le míg be nem zárom az egész programot.
        thread.Start(); // A Thread, azaz a külön program elindul.

        int speed = 1000; // Normál sebesség beállítva 1 mp-re
        bool paused = false; // Először nincs megállás
        int aksi = 100; // Akkumulátor maxra, azaz 100-ra állítva
        int v = 2; // Normál sebesség maga
        int aksiszamlalotolt = 0;
        int aksiszamlalomerul = 0;
        bool standby = false;
        int standbyszamlalo = 0;
        int logszamlalo = 2000;
        int gyujtoszamlalo = 0; // Ez számlálja a már összegyüjtött kincseket
        int gyujtendoszamlalo = 390; // Ez számlálja, hogy még mennyi van hátra
        int gyujteszsamlalo = 0; // Ez a bányászás idejét számolja
        int leptem = 0;
        

        // Kiírom a debugger-be a grid tartalmát

        PrintGrid(grid);

        // Meghatározom a kezdőpontot és a végpontot

        (int startX, int startY, List<(int x, int y)> goals) = FindStartAndGoals(grid);

        // Meghatározom a kezdéstől az útvonal algoritmusát

        List<(int x, int y)> teljespath = new List<(int x, int y)>(); // Egy teljes útvonal tartalmazására létrejött üres lista, majd beállítjuk a rover kezdőpontját
        int roverX = startX;
        int roverY = startY;

        // Amíg még vannak célok, amiket még meg kell látogatni, avagy be kell szednünk
        // Addig keressük a legközelebbi célpontot hozzánk képest, és egy útvonalat is a FindPath function alapján
        // A Math.Abs, kivonva a cél X és Y-át a rover X és Y-ából, megadja, hogy milyen messze van tőlünk a legközelebbi célpont szerinte, mivelhogy nem veszi figyelembe az akadályokat, tehát ez egyféle becslés
        // A First() függvény az elsőt keresi a sorrendben, amit az OrderBy() függvény rendezett nekünk
        // A goal => rész minden egyes célra számolja, hogy mennyire közel van hozzánk
        while(goals.Count > 0)
        {
            (int x, int y) celpont = goals.OrderBy(goal => Math.Abs(goal.x - roverX) + Math.Abs(goal.y - roverY)).First();
            List<(int x, int y)> pathtoCel = FindPath(grid, roverX, roverY, new List<(int x, int y)> { celpont });

            // Hogyha a FindPath function elérhetetlen célt talál, akkor break
            if(pathtoCel.Count == 0)
            {
                Console.Write("Nincs elérhető cél!");
                break;
            }
            teljespath.AddRange(pathtoCel.Skip(1)); // A teljes útvonalhoz hozzáadjuk a célhoz vezető útvonalat, átskipelve a kezdőpontot

            // Ha a rover eléri a célt, akkor a célok közül kivesszük ezt a begyűjtött elemet.
            // (Majd később az AnimatePath-nál kell elintézni, hogy bejárható mezővé tesszük a begyűjtött Y-okat és B-ket és G-ket, mert különben el fognak tűnni az összes ilyen betű a start-nál, és az nem jó
            roverX = celpont.x;
            roverY = celpont.y;
            //grid[roverY, roverX] = '.';
            goals.Remove(celpont);
        }

        // A FindPath function alapján egy visszavezető útvonalat hívunk meg
        // Amíg a visszavezető út nem teljesen bejárt, addig adja azt hozzá a teljes útvonalhoz, kihagyva az első pontot
        List<(int x, int y)> backpath = FindPath(grid, roverX, roverY, new List<(int x, int y)> { (startX, startY) });
        if(backpath.Count > 0)
        {
            teljespath.AddRange(backpath.Skip(1));
        }

        // Animáció

        AnimatePath(grid, teljespath, infoResz, infoCel, speed, paused, ref aksi, v, infoAksi, ref aksiszamlalotolt, ref aksiszamlalomerul, standby, ref standbyszamlalo, infoLocation, infoSebesseg, infoPause, ref logszamlalo, ref gyujtoszamlalo, ref gyujtendoszamlalo, ref gyujteszsamlalo, ref leptem); // A ref az arra jó, hogy ne csak az adott változók másolatát módosítsam, amely által nem számolna pontosan a program, hanem magát az adott változókat módosítsam és ne csak a másolatokat


        Console.ReadKey();
    }
    static char[,] LoadGrid(string[] lines)
    {
        // Meghatározom a magasságot és a hosszát a fájlnak
        int height = lines.Length;
        int width = lines[0].Split(',').Length;
        char[,] grid = new char[height, width];

        // Berakom a fájl karaktereket a grid-be.
        // Mivel a fájlban a karakterek vesszővel vannak elválasztva, ezért nekem split-el ki kell vennem a vesszőket a map-ról, különben probléma lehet a pathfinding mozgásában.
        for (int y = 0; y < height; y++)
        {
            string[] cellak = lines[y].Split(',');
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = cellak[x].Trim()[0]; // Megakadályozom a vessző és __space tagolás__ problémát a fájlban.
            }
        }
        return grid;
    }
    static void PrintGrid(char[,] grid)
    {
        //Console.Write("Grid tartalma:");

        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Console.Write(grid[y, x]);
            }
            Console.WriteLine();
        }
    }
    static (int, int, List<(int, int)>) FindStartAndGoals(char[,] grid)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        int startX = 0;
        int startY = 0;

        List<(int x, int y)> goals = new List<(int x, int y)>(); //<()> azt jelenti, hogy több értéket tárolok a listámban

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[y, x] == 'S')
                {
                    startX = x;
                    startY = y;
                }
                else if (grid[y, x] == 'G' || grid[y, x] == 'Y' || grid[y, x] == 'B')
                {
                    goals.Add((x, y));
                }
            }
        }
        Console.SetCursorPosition(width + 2, height + 2);
        Console.Write($"Start: {startX}, {startY}                    ");
        Console.SetCursorPosition(width + 2, height + 3);
        Console.Write($"Ásványkincsek száma: {goals.Count}              ");

        return (startX, startY, goals);
    }
    static List<(int x, int y)> FindPath(char[,] grid, int startX, int startY, List<(int x, int y)> goals)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        int endX = -1;
        int endY = -1; // Még nincs megtalálva a cél, se az X tengelyen, se az Y tengelyen

        // Útvonalat tervezek, azaz bool-al meghatározom, hogy melyik mezőt látogattuk és melyiket nem.
        // A start pozíciótól kezdünk, amint az látogatott.

        bool[,] visited = new bool[height, width];
        (int x, int y)[,] parent = new (int x, int y)[height, width];

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                parent[row, column] = (-1, -1);
            }
        }
        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();

        queue.Enqueue((startX, startY));
        visited[startY, startX] = true;

        // Megadom az irányokat, amerre mehetünk egy 2D-s téren.
        // 1,0 jelenti az x+1, y+0, azaz jobb irányt.
        // -1,0 jelenti az x-1, y+0, azaz a bal irányt.
        // 0,1 jelenti az x+0, y+1, azaz a lefelé irányt.
        // 0,-1 jelenti az x+0, y-1, azaz a felfelé irányt.
        // 1, 1 jelenti az x+1, y+1, azaz a jobbra és a lefelé átlós irányt.
        // 1, -1 jelenti az x+1, y-1, azaz a jobbra és felfelé átlós irányt.
        // -1, 1 jelenti az x-1, y+1, azaz a balra és lefelé átlós irányt.
        // -1, -1 jelenti az x-1, y-1, azaz a balra és felfelé átlós irányt.
        int[,] directions = {
            {1, 0},
            {-1, 0},
            {0, 1},
            {0, -1},
            {1, 1},
            {1, -1},
            {-1, 1},
            {-1, -1}
        };

        // Amíg van felfedezetlen mező, addig a jelenlegi helyet, amelyen állunk, dequeue-ezze, azaz vegye el a felfedezettlen területekből.
        // Ezzel megtudjuk, hogy hol vagyunk jelenleg.
        // És azzal, hogy az x és az y koordinátákat a jelenlegi hellyel, amelyen állunk, tesszük egyenlővé, azzal a kényelmesebb hozzáférést biztosítunk a kód további részéhez.
        // Egyben megvizsgáljuk, hogy a szomszédos mezők közül melyek felfedezettek és melyek nem és hogy a map-on belül vannak-e a szomszédos mezők, és hogy hol vannak akadályok.
        // És utána, ha a jelenlegi hely, amelyen állunk, az x-en és az y-on is a cél állomás koordinátáival egyenlő, akkor írja ki, hogy a célt elértük, és a ciklusnak/loop-nak legyen vége, mivelhogy elértük a célunkat.
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            int x = current.x;
            int y = current.y;

            if (goals.Contains((x, y)))
            {
                endX = x;
                endY = y;
                break;
            }
            for (int i = 0; i < 8; i++)
            {
                int nx = x + directions[i, 0];
                int ny = y + directions[i, 1];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (!visited[ny, nx] && grid[ny, nx] != '#')
                    {
                        visited[ny, nx] = true;
                        queue.Enqueue((nx, ny));
                        parent[ny, nx] = (x, y);
                    }
                }
            }
        }
        if(endX == -1 && endY == -1)
        {
            return new List<(int x, int y)>();
        }
        List<(int x, int y)> path = new List<(int x, int y)>();
        int px = endX;
        int py = endY;

        while (px != -1 && py != -1)
        {
            path.Add((px, py));
            (px, py) = parent[py, px];
        }

        path.Reverse();

        return path;
    }
    static void AnimatePath(char[,] grid, List<(int x, int y)> teljespath, int infoResz, int infoCel, int speed, bool paused, ref int aksi, int v, int infoAksi, ref int aksiszamlalotolt, ref int aksiszamlalomerul, bool standby, ref int standbyszamlalo, int infoLocation, int infoSebesseg, int infoPause, ref int logszamlalo, ref int gyujtoszamlalo, ref int gyujtendoszamlalo, ref int gyujtesszamlalo, ref int leptem)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        bool isMining = false;
        int miningTimer = 0;

        (int prevX, int prevY) = (-1, -1); // Itt tároljuk, hogy hol voltunk, mielőtt elmozdultunk volna

        //Console.Clear();
        // Ez a kód rész a kezdeti map-ot kiírja, mivelhogy a Console.Clear()-el mindig frissítjük a map-ot ahogy mozgunk

        int prevHeight = Console.WindowHeight;
        int prevWidth = Console.WindowWidth;

        void DrawMap()
        {
            Console.SetCursorPosition(0, 0);
            for (int row = 0; row < grid.GetLength(0); row++)
            {
                for (int col = 0; col < grid.GetLength(1); col++)
                {
                    Console.Write(grid[row, col]);
                }
                Console.WriteLine();
            }
        }
        DrawMap();
        
        foreach (var step in teljespath)
        {
            if (logszamlalo >= 2000) // Adatok kiírása minden fél órában (2 másodperc), addig amíg le van állítva
            {
                Console.SetCursorPosition(0, infoAksi);
                Console.Write($"Akkumulátor szint: {aksi}%     ");

                Console.SetCursorPosition(0, infoLocation);
                Console.Write($"Rover helye (x,y): {prevX}, {prevY}       ");

                Console.SetCursorPosition(width + 2, height + 4);
                Console.Write($"Gyűjtött kincsek száma: {gyujtoszamlalo}          ");

                Console.SetCursorPosition(width + 2, height + 5);
                Console.Write($"Gyűjtendő elemek: {gyujtendoszamlalo}              ");

                Console.SetCursorPosition(width + 2, height + 6);
                Console.Write($"A rover lépés száma: {leptem}                          ");

                Console.SetCursorPosition(0, infoSebesseg);
                if (paused == true)
                {
                    Console.Write("Sebesség: Leállítva     ");

                }
                else if (speed == 1000)
                {
                    Console.Write("Sebesség: Normál       ");
                }
                else if (speed == 667)
                {
                    Console.Write("Sebesség: Gyors        ");
                }
                else if (speed == 2000)
                {
                    Console.Write("Sebesség: Lassú        ");
                }

                Console.SetCursorPosition(0, infoPause);
                if (paused == true)
                {
                    Console.Write("Paused? Yes     ");
                }
                else if (paused == false)
                {
                    Console.Write("Paused? No     ");
                }

                logszamlalo = 0;
            }
            while (isMining == true)
            {
                int idougras = 100;
                Console.SetCursorPosition(0, infoPause);
                Console.Write("Paused? Yes (Mining)           ");

                if (Console.KeyAvailable)
                {
                    ConsoleKey k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.S)
                    {
                        speed = 2000;
                        v = 1;
                    }
                    else if (k == ConsoleKey.N)
                    {
                        speed = 1000;
                        v = 2; 
                    }
                    else if (k == ConsoleKey.F)
                    { 
                        speed = 667; 
                        v = 3; 
                    }
                    else if (k == ConsoleKey.Escape) 
                    {
                        paused = !paused; 
                    }
                }
                if(paused == false)
                {
                    System.Threading.Thread.Sleep(idougras);
                    miningTimer += idougras;
                    logszamlalo += idougras;

                    if (aksitolt == true)
                    {
                        aksiszamlalotolt += idougras;
                        if (aksiszamlalotolt >= 2000)
                        {
                            aksi += 10;
                            aksiszamlalotolt = 0;
                            if (aksi > 100)
                            {
                                aksi = 100;
                            }
                        }
                    }

                    if (miningTimer >= 2000)
                    {
                        aksi -= 2;
                        if(aksi <= 0)
                        {
                            aksi = 0;
                        }
                        isMining = false;
                        miningTimer = 0;
                        Console.SetCursorPosition(0, infoPause);
                        Console.Write("Paused? No            ");
                    }
                }
                else if(paused == true)
                {
                    System.Threading.Thread.Sleep(50);
                    if(aksitolt == true)
                    {
                        aksiszamlalotolt += 50;
                        if(aksiszamlalotolt >= 2000)
                        {
                            aksi += 10;
                            if(aksi > 100)
                            {
                                aksi = 100;
                            }
                            aksiszamlalotolt = 0;
                        }
                    }
                }
                Console.SetCursorPosition(0, infoAksi);
                Console.Write($"Akkumulátor szint: {aksi}%     ");
            }

            leptem += 1;
            if(Console.WindowHeight != prevHeight || Console.WindowWidth != prevWidth)
            {
                try
                {
                    Console.SetBufferSize(width + 10, height + 10);
                    Console.SetWindowSize(width + 10, height + 10);
                }
                catch
                {

                }
                
                DrawMap();

                if(prevX != -1 && prevY != -1)
                {
                    Console.SetCursorPosition(prevX, prevY);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write('&');
                    Console.ResetColor();

                }
                prevHeight = Console.WindowHeight;
                prevWidth = Console.WindowWidth;
            }
            // A program automatikusan fut, még akkor is ha lenyomok vagy nem nyomok le egy gombot, azaz akkor is ha módosítom a sebességet vagy nem módosítom a sebességet. Ezt jelenti a KeyAvaible.
            // Viszont ha lenyomom az S-t, akkor lassan haladok, azaz 1 blokk 2 mp-ként (fél óra).
            // Ha az N-t, akkor normálisan haladok, azaz 1 blokk 1 mp-ként (2 blokk fél óra).
            // És ha az F-et, akkor gyorsan haladok, azaz 1 blokk 0.75 mp-ként (3 blokk fél óra).
            if (Console.KeyAvailable)
            {
                ConsoleKey consolekey = Console.ReadKey(true).Key;
                switch (consolekey)
                {
                    case ConsoleKey.S:
                        speed = 2000;
                        v = 1;
                        break;
                    case ConsoleKey.N:
                        speed = 1000;
                        v = 2;
                        break;
                    case ConsoleKey.F:
                        speed = 667;
                        v = 3;
                        break;
                    case ConsoleKey.Escape:
                        paused = !paused;
                        standby = paused;
                        break;
                }
            }

            
            // Amíg megállítom az algoritmust a Space gomb megnyomásával, addig az algoritmus nem fog mozogni.
            // Majd ha újra lenyomom a Space-t, akkor mozog az algoritmus, mivelhogy a paused-ot false-ra állítom.
            while (paused)
            {
                Console.SetCursorPosition(0, infoPause);
                if (paused == true)
                {
                    Console.Write("Paused? Yes     ");
                }
                if (Console.KeyAvailable)
                {
                    ConsoleKey stop = Console.ReadKey(true).Key;
                    if (stop == ConsoleKey.Escape)
                    {
                        paused = false;
                        standby = false;
                        Console.SetCursorPosition(0, infoPause);
                        if (paused == false)
                        {
                            Console.Write("Paused? No     ");
                        }
                    }
                }
                System.Threading.Thread.Sleep(50); // 50 miliszekundumot várunk, hogy megálljon, így a teljesítmény is javul és a CPU nem dolgozik 100%-on pause közben
                standbyszamlalo += 50;
                logszamlalo += 50;
                gyujtesszamlalo += 50;

                if (aksitolt == true)
                {
                    aksiszamlalotolt += 50;
                    if(aksiszamlalotolt >= 2000)
                    {
                        aksi += 10;
                        if(aksi > 100)
                        {
                            aksi = 100;
                        }
                        aksiszamlalotolt = 0;
                    }
                }
                /*if(gyujtesszamlalo >= 2000 && paused == true) // Minden fél órában (2 másodperc), amíg bányászunk, vonjon le 1 egységet, az ötlet az az, hogy a standby számolása automatikusan levon 1, amikor állunk, tehát ha a bányászás is levon 1 egységet, akkor 2 egység van levonva fél óra alatt csupán bányászásért
                {
                    aksi -= 1;
                    paused = false;
                }*/
                if(standbyszamlalo >= 2000 && standby == true) // Minden fél órában (2 másodperc), amíg állunk, vonjon le 1 egységet
                {
                    aksi -= 1;
                    if (aksi <= 0)
                    {
                        aksi = 0;
                    }
                    standbyszamlalo = 0;
                }
                if(aksi > 0 && standby == false)
                {
                    paused = false;
                }
                Console.SetCursorPosition(0, infoAksi);
                Console.Write($"Akkumulátor szint: {aksi}%      ");
            }

            int x = step.x;
            int y = step.y;

            // Ha elhagytuk a mezőt, amelyen előbb voltunk, írja át a pozíciónkat
            if (prevX != -1 && prevY != -1)
            {
                Console.SetCursorPosition(prevX, prevY);
                Console.Write(grid[prevY, prevX]);
            }

            // Megjelenünk az új helyen egy '&'-ként és pirosan, de nem hagyjuk, hogy az egész map piros legyen, erre van a ResetColor()
            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write('&');
            Console.ResetColor();

            prevX = x;
            prevY = y;

            if (grid[y, x] == 'G' || grid[y, x] == 'B' || grid[y, x] == 'Y')
            {
                grid[y, x] = '.';
                gyujtoszamlalo += 1;
                gyujtendoszamlalo -= 1;
                isMining = true;
                miningTimer = 0;
            }


            // Zöld utasítás
            Console.SetCursorPosition(0, infoResz);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("S = lassú sebességért || N = normál sebességért || F = gyors sebességért || Esc = megállás");
            Console.ResetColor();

            
            
            System.Threading.Thread.Sleep(speed); // Ha semmi sincs megnyomva, addig haladjon tovább a beállított sebességgel

            aksiszamlalomerul += speed;
            logszamlalo += speed;
            if(aksiszamlalomerul >= 2000) // A sebesség idejét hozzáadva a bool-hoz, és így 2 másodpercenként (fél óra) merül az aksi.
            {
                aksi -= (2 * (v * v));
                if(aksi <= 0)
                {
                    aksi = 0;
                }
                aksiszamlalomerul = 0;
            }
                

            if(aksi <= 0)
            {
                aksi = 0;
                paused = true;
                Console.SetCursorPosition(0, height + 7);
                Console.Write("Autó-megállás lemerülés miatt. Nyomd meg az Esc-et elindulásért!                ");
                if(aksitolt == true)
                {
                    Console.SetCursorPosition(0, height + 9);
                    Console.Write("Töltés folyamatban!           ");
                }
                else if(aksitolt == false)
                {
                    Console.SetCursorPosition(0, height + 9);
                    Console.Write("Várj nappalig!              ");
                }
                continue;
            }

            if (aksitolt)
            {
                aksiszamlalotolt += speed;
                if (aksiszamlalotolt >= 2000) // Ha nappal van, a sebesség idejét hozzáadva a bool-hoz, és így 2 másodpercenként (fél óra) tölt az aksi.
                {
                    aksi += 10;
                    aksiszamlalotolt = 0;
                }
            }


            if (aksi > 100)
            {
                aksi = 100;
            }
            if(aksi <= 20)
            {
                Console.SetCursorPosition(0, height + 8);
                Console.Write("20% - Alacsony töltöttségi szint!      ");
            }
            else if(aksi >= 20)
            {
                Console.SetCursorPosition(0, height + 8);
                Console.Write("                                        ");
            }
            

            
            
        }
        Console.SetCursorPosition(0, infoCel);
        Console.Write("Cél elérve!");
    }
}