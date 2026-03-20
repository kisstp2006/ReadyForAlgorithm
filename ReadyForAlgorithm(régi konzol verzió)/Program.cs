using System.Diagnostics;
using System.Text;
using ReadyForAlgorithm.Core;

namespace Program;

internal static class Program
{
    static volatile bool aksitolt = false; // volatile az azért felelős, hogy mindig friss adatunk legyen, és ne csússzon el semmi az aksitolt kapcsán
    static void Main()
    {
        Console.WriteLine("Hello world!");
        string[] lines = File.ReadAllLines("mars_map_50x50.csv"); // Beolvasom a fájlt

        Console.CursorVisible = false;
        Stopwatch stopwatch = Stopwatch.StartNew();
        long lastTick = stopwatch.ElapsedMilliseconds;
        bool exitRequested = false;


        // Csinálok egy 2D-s grid-et magassággal és hosszal. A vessző a zárójelben adja meg, hogy 2D-s grid-ről beszélünk.

        char[,] grid = LoadGrid(lines);


        // Egy nap számolásához szükéges elemek és a kiírott szövegek elhelyezése

        int day = 0;
        bool night = true;

        int height = grid.GetLength(0);

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
                        Console.SetCursorPosition(0, infoTime); // A villogó kis téglalapot elállítom, ahol az infoTime van az Y tengelyen, azaz a mapom alá 2 sorral
                        Console.Write("Éjszaka van    "); // 64 másodperc = 16 óra és 96 másodperc = 24 óra
                        // Azért raktam jó pár Space-t a kiírás után, mert teszt során láttam, hogy az Éjszaka felirat hosszabb, mint a Nappal felirat.
                        // Tehát valami az Éjszakából hátramaradt a Nappalra, azaz 'Nappal vann' volt a kiírás, amit így javítottam ki.
                        aksitolt = false; // Ne töltsön az aksi este
                    }
                    night = true;
                }
                else if (day >= 0 && day <= 64)
                {
                    if (night)
                    {
                        Console.SetCursorPosition(0, infoTime); // A villogó kis téglalapot elállítom, ahol az infoTime van az Y tengelyen, azaz a mapom alá 2 sorral
                        Console.Write("Nappal van    "); // 0 másodperc = hajnal/0 óra és 64 másodperc = 16 óra
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
        int logszamlalo = 0;
        

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
        // A Math.Abs, kivonva a cél X és Y-át a rover X és Y-ából, megadja, hogy milyen messze van tőlünk a legközelebbi célpont
        // A First() függvény az elsőt keresi a sorrendben, amit az OrderBy() függvény rendezett nekünk
        // A goal => rész minden egyes célra számolja, hogy mennyire közel van hozzánk
        while(goals.Count > 0)
        {
            (int x, int y) celpont = goals.OrderBy(goal => Math.Abs(goal.x - roverX) + Math.Abs(goal.y - roverY)).First();
            List<(int x, int y)> pathtoGoal = FindPath(grid, roverX, roverY, new List<(int x, int y)> { celpont });

            // Hogyha a FindPath function elérhetetlen célt talál, akkor break
            if(pathtoGoal.Count == 0)
            {
                Console.Write("Nincs elérhető cél!");
                break;
            }
            teljespath.AddRange(pathtoGoal.Skip(1));

            roverX = celpont.x;
            roverY = celpont.y;
            grid[roverY, roverX] = '.';
            goals.Remove(celpont);
        }
        List<(int x, int y)> backpath = FindPath(grid, roverX, roverY, new List<(int x, int y)> { (startX, startY) });
        if(backpath.Count > 0)
        {
            teljespath.AddRange(backpath.Skip(1));
        }

        // Animáció

        AnimatePath(grid, teljespath, infoResz, infoCel, speed, paused, ref aksi, v, infoAksi, ref aksiszamlalotolt, ref aksiszamlalomerul, standby, ref standbyszamlalo, infoLocation, infoSebesseg, infoPause, ref logszamlalo);
        
        
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
        Console.Write("Grid tartalma:");

        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Console.Write(grid[y, x]);
            }
        }
    }

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
        Console.Write($"Start: {startX}, {startY}");
        Console.SetCursorPosition(width + 2, height + 3);
        Console.Write($"Gyűjtött elemek: {goals.Count}");

        AppendMap(builder, snapshot);
        builder.AppendLine();
        builder.AppendLine($"Battery: {snapshot.Battery}%");
        builder.AppendLine($"Speed: {GetSpeedLabel(snapshot.SpeedMode)}");
        builder.AppendLine($"Paused: {(snapshot.IsPaused ? "Yes" : "No")}");
        builder.AppendLine($"Time: {snapshot.TimeLabel}");
        builder.AppendLine($"Position: {snapshot.RoverPosition.X}, {snapshot.RoverPosition.Y}");
        builder.AppendLine($"Samples: {snapshot.CollectedGoalCount}/{snapshot.TotalGoalCount}");
        builder.AppendLine($"Remaining goals: {snapshot.RemainingGoals.Count}");
        builder.AppendLine();
        builder.AppendLine("Recent log:");

        foreach (RoverLogEntry log in snapshot.Logs.TakeLast(10))
        {
            builder.AppendLine($"[{FormatLogTime(log.Tick)}] {log.Message}");
        }

        queue.Enqueue((startX, startY));
        visited[startY, startX] = true;

        // Megadom az irányokat, amerre mehetünk egy 2D-s téren.
        // 1,0 jelenti az x+1, y+0, azaz jobb irányt.
        // -1,0 jelenti az x-1, y+0, azaz a bal irányt.
        // 0,1 jelenti az x+0, y+1, azaz a lefelé irányt.
        // 0,-1 jelenti az x+0, y-1, azaz a felfelé irányt.
        // 1, 1 jelenti az x+1, y+1, azaz a balra és a lefelé átlós irányt.
        // 1, -1 jelenti az x+1, y-1, azaz a balra és felfelé átlós irányt.
        // -1, 1 jelenti az x-1, y+1, azaz a jobbra és lefelé átlós irányt.
        // -1, -1 jelenti az x-1, y-1, azaz a jobbra és felfelé átlós irányt.
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
    static void AnimatePath(char[,] grid, List<(int x, int y)> teljespath, int infoResz, int infoCel, int speed, bool paused, ref int aksi, int v, int infoAksi, ref int aksiszamlalotolt, ref int aksiszamlalomerul, bool standby, ref int standbyszamlalo, int infoLocation, int infoSebesseg, int infoPause, ref int logszamlalo)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        

        (int prevX, int prevY) = (-1, -1); // Itt tároljuk, hogy hol voltunk, mielőtt elmozdultunk volna

        Console.Clear();
        // Ez a kód rész a kezdeti map-ot kiírja, mivelhogy a Console.Clear()-el mindig frissítjük a map-ot ahogy mozgunk
        for (int row = 0; row < grid.GetLength(0); row++)
        {
            for (int col = 0; col < grid.GetLength(1); col++)
            {
                Console.Write(grid[row, col]);
            }
            Console.WriteLine();
        }

        
        foreach (var step in teljespath)
        {

            // A program automatikusan fut, még akkor is ha lenyomok vagy nem nyomok le egy gombot, azaz akkor is ha módosítom a sebességet vagy nem módosítom a sebességet. Ezt jelenti a KeyAvaible.
            // Viszont ha lenyomom az S-t, akkor lassan haladok, azaz 1 blokk 2 mp-ként (fél óra).
            // Ha az N-t, akkor normálisan haladok, azaz 1 blokk 1 mp-ként (2 blokk fél óra).
            // És ha az F-et, akkor gyorsan haladok, azaz 1 blokk 0.75 mp-ként (3 blokk fél óra).
            if (Console.KeyAvailable)
            {
                ConsoleKey consolekey = Console.ReadKey(true).Key;
                if (consolekey == ConsoleKey.S)
                {
                    speed = 2000;
                    v = 1;
                }
                else if (consolekey == ConsoleKey.N)
                {
                    speed = 1000;
                    v = 2;
                }
                else if (consolekey == ConsoleKey.F)
                {
                    speed = 667;
                    v = 3;
                }
                else if (consolekey == ConsoleKey.Escape)
                {
                    paused = true;
                    standby = true;
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
                if(standbyszamlalo >= 2000 && standby == true) // Minden fél órában (2 másodperc), amíg állunk, vonjon le 1 egységet
                {
                    aksi -= 1;
                    if (aksi <= 0)
                    {
                        aksi = 0;
                    }
                    standbyszamlalo = 0;
                }
                if(logszamlalo >= 2000) // Adatok kiírása minden fél órában (2 másodperc), addig amíg le van állítva
                {
                    Console.SetCursorPosition(0, infoAksi);
                    Console.Write($"Akkumulátor szint: {aksi}     ");

                    Console.SetCursorPosition(0, infoLocation);
                    Console.Write($"Rover helye (x,y): {prevX}, {prevY}       ");

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
            }

            int x = step.x;
            int y = step.y;

                if (current == snapshot.RoverPosition)
                {
                    builder.Append('&');
                    continue;
                }

            // Megjelenünk az új helyen egy '&'-ként és pirosan, de nem hagyjuk, hogy az egész map piros legyen, erre van a ResetColor()
            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write('&');
            Console.ResetColor();

            prevX = x;
            prevY = y;


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
                aksiszamlalomerul = 0;
            }
            if(logszamlalo >= 2000) // Adatok kiírása minden fél órában (2 másodperc), addig amíg megyünk
            {
                Console.SetCursorPosition(0, infoAksi);
                Console.Write($"Akkumulátor szint: {aksi}     ");

                Console.SetCursorPosition(0, infoLocation);
                Console.Write($"Rover helye (x,y): {prevX}, {prevY}       ");

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

            if (aksitolt)
            {
                aksiszamlalotolt += speed;
                if(aksiszamlalotolt >= 2000) // Ha nappal van, a sebesség idejét hozzáadva a bool-hoz, és így 2 másodpercenként (fél óra) tölt az aksi.
                {
                    aksi += 10;
                    aksiszamlalotolt = 0;
                }
            }

            if(aksi <= 0)
            {
                aksi = 0;
                paused = true;
                Console.SetCursorPosition(0, height + 7);
                Console.Write("Autó-megállás lemerülés miatt. Nyomd meg az Esc-et elindulásért!"    );
                if(aksitolt == true)
                {
                    Console.SetCursorPosition(0, height + 9);
                    Console.Write("Töltés folyamatban!           ");
                }
                else if(aksitolt == false)
                {
                    Console.SetCursorPosition(0, height + 9);
                    Console.Write("Várj nappalig!"              );
                }
            }
            if(aksi >= 100)
            {
                aksi = 100;
            }
            if(aksi <= 20)
            {
                Console.SetCursorPosition(0, height + 8);
                Console.Write("20% - Alacsony töltöttségi szint!"      );
            }
            

            
            
        }
        Console.SetCursorPosition(0, infoCel);
        Console.Write("Cél elérve!");
    }
}