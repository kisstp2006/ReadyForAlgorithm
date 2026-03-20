═══════════════════════════════════════════════════════════════════════════════
                        MARS ROVER PATHFINDING SZIMULÁCIÓ
═══════════════════════════════════════════════════════════════════════════════

1. CSAPATINFORMÁCIÓK
═══════════════════════════════════════════════════════════════════════════════

Csapat neve: TH++

Csapattagok:
  - Kiss Tibor Péter, Debreceni SZC Beregszászi Pál Technikum és Kollégium
  - Bereczki Hunor, Debreceni SZC Beregszászi Pál Technikum és Kollégium

Felkészítő tanár: Szabó Anett

Kapcsolat: kisstp2006@gmail.com


2. PROGRAMFEJLESZTŐI KÖRNYEZET
═══════════════════════════════════════════════════════════════════════════════

Fejlesztői környezet:
  - Visual Studio 2022 (vagy újabb verzió)
  - .NET 6.0 SDK

Használt technológiák:
  - C# 10.0
  - ImGui.NET 1.91.6.1 (Modern grafikus felhasználói felület)
  - OpenTK 4.9.3 (OpenGL wrapper grafikai megjelenítéshez)

Projekt struktúra:
  - ReadyForAlgorithm.Core: Alapvető szimuláció logika
  - ReadyForAlgorithm.ImGui: Grafikus felhasználói felület


3. PROGRAM HASZNÁLATI ÚTMUTATÓ
═══════════════════════════════════════════════════════════════════════════════

3.1. TELEPÍTÉS ÉS INDÍTÁS
───────────────────────────────────────────────────────────────────────────────

A) Forráskódból való fordítás:
   1. Csomagold ki a ZIP fájlt
   2. Nyisd meg a solution-t Visual Studio-ban
   3. Build > Build Solution (Ctrl+Shift+B)
   4. Futtasd a ReadyForAlgorithm.ImGui projektet

B) Futtatható fájl használata:
   1. Navigálj a ReadyForAlgorithm.ImGui\bin\Debug\net6.0\win-x64\ mappába
   2. Indítsd el a ReadyForAlgorithm.ImGui.exe fájlt

Parancssori argumentumok (opcionális):
   ReadyForAlgorithm.ImGui.exe [map_fájl_útvonala] [küldetés_időtartama_órákban]
   
   Példa: ReadyForAlgorithm.ImGui.exe mars_map_50x50.csv 48


3.2. GRAFIKUS FELÜLET (ImGui verzió)
───────────────────────────────────────────────────────────────────────────────

INDÍTÁSI KÉPERNYŐ:
  - Beállítható a küldetés időtartama (minimum 24 óra)
  - "BEGIN MISSION" gombbal indítható a szimuláció

VEZÉRLŐPULT (Mission Control):
  - Sebességváltó gombok:
    * Slow: Lassú sebesség (1 mező = 2 másodperc, 1 blokk/fél óra)
    * Normal: Normál sebesség (1 mező = 1 másodperc, 2 blokk/fél óra)
    * Fast: Gyors sebesség (1 mező = 0.67 másodperc, 3 blokk/fél óra)
  - Pause/Resume gomb: Szimuláció szüneteltetése/folytatása

TÉRKÉP ABLAK (Map Grid):
  - Megjeleníti a Mars felszínét
  - Színkódok:
    * & (piros): Rover jelenlegi pozíciója
    * S (cián): Kezdőpont
    * G (zöld): Gyűjthető minta
    * Y (sárga): Gyűjthető minta
    * B (kék): Gyűjthető minta
    * # (sötétszürke): Akadály (szikla)
    * . (világosszürke): Szabad terület

TELEMETRIA ABLAK (Telemetry):
  - MISSION TIME: Hátralévő küldetési idő órákban
  - Battery: Akkumulátor töltöttségi szint (%)
  - Time: Marsi idő (Nappal/Éjszaka)
  - Position: Rover koordinátái (X, Y)
  - Speed: Jelenlegi sebesség mód
  - Status: Futási állapot (Running/Paused)
  - Samples: Összegyűjtött minták száma
  - Remaining: Még összegyűjtendő minták

KONZOL LOG ABLAK (Console Log):
  - Valós idejű eseménynapló
  - Színkódolt üzenetek:
    * Zöld: Mintagyűjtés események
    * Sárga: Akkumulátor figyelmeztetések
    * Kék: Sebesség és státusz változások
    * Piros: Kritikus események


3.3. KONZOL VERZIÓ
───────────────────────────────────────────────────────────────────────────────

BILLENTYŰPARANCSOK:
  - S: Lassú sebesség
  - N: Normál sebesség
  - F: Gyors sebesség
  - ESC: Szüneteltetés/Folytatás

KÉPERNYŐ INFORMÁCIÓK:
  - Térképet jelenít meg a konzol tetején
  - Rover pozíciója: & karakter (piros színnel)
  - Alul információs sáv:
    * Nappali/Éjszakai ciklus
    * Akkumulátor szint
    * Rover helyzete
    * Sebesség
    * Szünet állapot


4. JÁTÉKMECHANIKA
═══════════════════════════════════════════════════════════════════════════════

KÜLDETÉS CÉLJA:
  - Gyűjts össze minden mintát (G, Y, B) a térképen
  - Térj vissza a kezdőponthoz (S)
  - Tartsd be a küldetés időkorlátját

ENERGIAGAZDÁLKODÁS:
  - Kezdő akkumulátor: 100%
  - Energiafogyasztás:
    * Lassú sebesség: 2 energia/fél óra
    * Normál sebesség: 8 energia/fél óra
    * Gyors sebesség: 18 energia/fél óra
    * Állóban (Standby): 1 energia/fél óra
  - Töltés nappal: +10 energia/fél óra
  - Éjjel nem töltődik az akkumulátor

NAP/ÉJSZAKA CIKLUS:
  - 1 marsi nap = 96 másodperc (valós idő)
  - Nappal: 0-64 másodperc (0-16 óra)
  - Éjszaka: 64-96 másodperc (16-24 óra)

JÁTÉK VÉGE:
  - Küldetés sikeres: Minden minta összegyűjtve, visszatérés a kezdőponthoz
  - Küldetés sikertelen: Időkorlát lejárt
  - Automatikus megállás: Akkumulátor lemerült (csak nappal töltődik újra)


5. ÚTVONALTERVEZŐ ALGORITMUS LEÍRÁSA
═══════════════════════════════════════════════════════════════════════════════

ALGORITMUS TÍPUSA: Breadth-First Search (BFS) módosított legközelebbi szomszéd 
                    stratégiával

MŰKÖDÉSI ELVE:

1. KEZDETI ÁLLAPOT:
   - Beolvassa a térképet CSV fájlból
   - Azonosítja a kezdőpontot (S) és a célpontokat (G, Y, B)
   - Létrehoz egy üres teljes útvonal listát

2. CÉLPONTOK BEJÁRÁSA (Greedy Nearest Neighbor stratégia):
   a) Amíg vannak még célpontok:
      - Keresi a legközelebbi célpontot a rover aktuális pozíciójától
      - Manhattan-távolság alapján: |célX - roverX| + |célY - roverY|
      - BFS algoritmussal útvonalat keres a legközelebbi célhoz
      - Hozzáadja az útvonalat a teljes útvonalhoz
      - Frissíti a rover pozícióját a cél pozíciójára
      - Eltávolítja a célpontot a lista-ból
      - Megjelöli a begyűjtött pontot '.' karakterrel a térképen
   
3. VISSZATÉRÉS A KEZDŐPONTHOZ:
   - BFS algoritmussal útvonalat keres vissza a kezdőponthoz
   - Hozzáadja a visszaút-ot a teljes útvonalhoz

4. BFS PATHFINDING RÉSZLETEI:
   
   Bemenet:
   - grid: A terep 2D tömbje
   - start: Kezdő pozíció
   - goals: Célpontok listája
   
   Működés:
   a) Inicializálás:
      - Létrehoz egy queue-t (sor adatszerkezet)
      - Visited tömb: nyomon követi a már meglátogatott mezőket
      - Parent tömb: tárolja minden mező szülő-pozícióját az útvonal-visszakövetéshez
   
   b) Felfedezés (BFS):
      - Kezdőpontot berakja a queue-ba
      - Amíg van elem a queue-ban:
        * Kiveszi az első elemet (current)
        * Ha current egy célpont, megtaláltuk az utat - befejezi a keresést
        * Különben megvizsgálja mind a 8 szomszédos mezőt:
          - 4 fő irány: fel, le, bal, jobb
          - 4 átlós irány: bal-fel, jobb-fel, bal-le, jobb-le
        * Minden érvényes, nem látogatott és nem akadály mezőt:
          - Megjelöli látogatottként
          - Berakja a queue-ba
          - Beállítja a parent értékét
   
   c) Útvonal rekonstrukció:
      - Ha van megoldás:
        * Visszafelé halad a parent linkeken a céltól a startig
        * Megfordítja a listát, hogy start-tól cél-ig legyen
      - Ha nincs megoldás: üres listát ad vissza

5. OPTIMALIZÁCIÓ:
   - Greedy megközelítés: mindig a legközelebbi célt választja
   - Ez nem garantáltan az optimális megoldás, de gyors és hatékony
   - A BFS garantálja a legrövidebb utat két pont között


6. PSZEUDO KÓD
═══════════════════════════════════════════════════════════════════════════════

FŐPROGRAM:
────────────────────────────────────────────────────────────────────────────────
KEZDET
    grid = BETÖLT_TÉRKÉPET()
    (start, célpontok) = KERESS_START_ÉS_CÉLOKAT(grid)
    
    teljes_útvonal = []
    rover_pozíció = start
    
    AMÍG (van még célpont) VÉGEZD
        legközelebbi_cél = KERESS_LEGKÖZELEBBI_CÉLT(rover_pozíció, célpontok)
        útvonal_célhoz = BFS_PATHFINDING(grid, rover_pozíció, legközelebbi_cél)
        
        HA (útvonal_célhoz üres) AKKOR
            KILÉP // Nincs elérhető cél
        VÉGE
        
        teljes_útvonal += útvonal_célhoz
        rover_pozíció = legközelebbi_cél
        grid[cél pozíció] = '.'
        TÁVOLÍTSD_EL(célpontok, legközelebbi_cél)
    VÉGE
    
    visszaút = BFS_PATHFINDING(grid, rover_pozíció, start)
    teljes_útvonal += visszaút
    
    ANIMÁLD(grid, teljes_útvonal)
VÉGE


BFS_PATHFINDING(grid, start, célok):
────────────────────────────────────────────────────────────────────────────────
KEZDET
    magasság = grid.magasság
    szélesség = grid.szélesség
    
    visited = LOGIKAI_TÖMB[magasság][szélesség] // Hamis értékekkel
    parent = POZÍCIÓ_TÖMB[magasság][szélesség] // (-1,-1) értékekkel
    queue = ÚJ_SOR()
    
    queue.HOZZÁAD(start)
    visited[start.y][start.x] = IGAZ
    
    irányok = [
        [1, 0],   // Jobb
        [-1, 0],  // Bal
        [0, 1],   // Le
        [0, -1],  // Fel
        [1, 1],   // Jobb-le (átlós)
        [1, -1],  // Jobb-fel (átlós)
        [-1, 1],  // Bal-le (átlós)
        [-1, -1]  // Bal-fel (átlós)
    ]
    
    cél_megtalálva = HAMIS
    vég_pozíció = (-1, -1)
    
    AMÍG (queue NEM üres ÉS NEM cél_megtalálva) VÉGEZD
        current = queue.KIVESZ()
        
        HA (current BENNE_VAN célok-ban) AKKOR
            vég_pozíció = current
            cél_megtalálva = IGAZ
            KILÉP
        VÉGE
        
        MINDEN irány-ra VÉGEZD
            következő_x = current.x + irány[0]
            következő_y = current.y + irány[1]
            
            HA (ÉRVÉNYES_POZÍCIÓ(következő_x, következő_y, szélesség, magasság)) AKKOR
                HA (NEM visited[következő_y][következő_x] ÉS 
                    grid[következő_y][következő_x] != '#') AKKOR
                    
                    visited[következő_y][következő_x] = IGAZ
                    queue.HOZZÁAD((következő_x, következő_y))
                    parent[következő_y][következő_x] = current
                VÉGE
            VÉGE
        VÉGE
    VÉGE
    
    HA (NEM cél_megtalálva) AKKOR
        VISSZAAD üres_lista
    VÉGE
    
    // Útvonal rekonstrukció
    útvonal = []
    pozíció = vég_pozíció
    
    AMÍG (pozíció != (-1, -1)) VÉGEZD
        útvonal.HOZZÁAD(pozíció)
        pozíció = parent[pozíció.y][pozíció.x]
    VÉGE
    
    útvonal.MEGFORDÍT()
    VISSZAAD útvonal
VÉGE


KERESS_LEGKÖZELEBBI_CÉLT(rover, célok):
────────────────────────────────────────────────────────────────────────────────
KEZDET
    legkisebb_távolság = VÉGTELEN
    legközelebbi = NULL
    
    MINDEN cél ÍN célok VÉGEZD
        távolság = |cél.x - rover.x| + |cél.y - rover.y| // Manhattan-távolság
        
        HA (távolság < legkisebb_távolság) AKKOR
            legkisebb_távolság = távolság
            legközelebbi = cél
        VÉGE
    VÉGE
    
    VISSZAAD legközelebbi
VÉGE


7. ALGORITMUS KOMPLEXITÁSA
═══════════════════════════════════════════════════════════════════════════════

Időbeli komplexitás:
  - Egy BFS keresés: O(V + E), ahol V = mezők száma, E = élek száma
  - Teljes küldetés: O(n * (V + E)), ahol n = célpontok száma
  - Legközelebbi cél keresése: O(n)

Térbeli komplexitás: O(V) a visited és parent tömbök miatt


8. SZIMULÁCIÓ RÉSZLETEK
═══════════════════════════════════════════════════════════════════════════════

IDŐSKÁLA:
  - 1 másodperc valós idő = 0.25 marsi óra
  - 4 másodperc valós idő = 1 marsi óra
  - 96 másodperc valós idő = 1 marsi nap (24 óra)

AKKUMULÁTOR:
  - Kezdő töltöttség: 100%
  - Töltés sebessége nappal: 10%/fél óra (2 másodperc)
  - Fogyasztás mozgás közben:
    * Lassú: 2%/fél óra
    * Normál: 8%/fél óra
    * Gyors: 18%/fél óra
  - Fogyasztás állás közben: 1%/fél óra
  - Figyelmeztetés 20% alatt
  - Automatikus megállás 0%-nál

JÁTÉK VÉGE FELTÉTELEK:
  - Küldetés időkorlát lejárt: GAME OVER ablak jelenik meg
  - Lehetőség a küldetés újraindítására a "RESTART MISSION" gombbal


9. FÁJLSTRUKTÚRA
═══════════════════════════════════════════════════════════════════════════════

ReadyForAlgorithm/
├── ReadyForAlgorithm.Core/          # Mag logika
│   ├── PathPlanner.cs               # Útvonaltervező algoritmus (BFS)
│   ├── RoverSimulation.cs           # Rover szimuláció logika
│   ├── MapLoader.cs                 # Térkép betöltés
│   └── Models/                      # Adatmodellek
├── ReadyForAlgorithm.ImGui/         # Grafikus frontend
│   ├── Program.cs                   # Főprogram és ImGui UI
│   ├── FontAwesome6.cs              # Ikonok
│   └── Fonts/                       # Font fájlok (Font Awesome)
└── mars_map_50x50.csv               # Alapértelmezett térkép


10. TECHNIKAI MEGJEGYZÉSEK
═══════════════════════════════════════════════════════════════════════════════

- A program UTF-8 kódolást használ a konzol kimeneten a speciális karakterek 
  helyes megjelenítéséhez
- A Font Awesome ikonok megjelenítéséhez szükséges a fa-solid-900.ttf fájl
  a Fonts/ mappában
- A .csproj fájlban be van állítva a win-x64 target platform


═══════════════════════════════════════════════════════════════════════════════
                              DOKUMENTÁCIÓ VÉGE
═══════════════════════════════════════════════════════════════════════════════
