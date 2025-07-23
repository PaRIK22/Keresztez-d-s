using System;
using System.Threading;

// Jelzőlámpa színek felsorolása
public enum LightColor
{
    Red,
    Red_Yellow,
    Green,
    Green_end,
    Yellow,
    OutOfOrder
}

// Rendszer állapotok felsorolása
public enum States
{
    Running,
    OutOfOrder,
    PedestSignedNS,
    PedestSignedEW
}

class Program
{

    // Aktuális lámpa állapot, amit a billentyűfigyelő módosíthat
    static volatile States LampState;
    static volatile States PedestLampState;

    // Lámpa állapotok kiírása
    static void PrintLampColors(
    LightColor ns, LightColor nsL, LightColor ew, LightColor ewL, LightColor ewP, LightColor nsP, bool nsright, bool ewright)
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"Észak-Dél: {ns}");
        Console.WriteLine($"Észak-Dél Kanyarodó: {nsL}");
        Console.WriteLine($"Kelet-Nyugat: {ew}");
        Console.WriteLine($"Kelet-Nyugat Kanyarodó: {ewL}");
        Console.WriteLine($"Észak-Dél-en keresztül gyalogos : {ewP}");
        Console.WriteLine($"Kelet-Nyugaton keresztül gyalogos : {nsP}");
        if (nsright)
            Console.WriteLine("Észak-Dél forgalom kanyarodhat jobbra.");
        if (ewright)
            Console.WriteLine("Kelet-Nyugat forgalom kanyarodhat jobbra.");
        Console.WriteLine("--------------------------------------------------");
    }

    // Jelzőlámpa vezérlési szekvencia
    public static void LampSequence()
    {
        

        LightColor[] ns = { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red };
        int[] nsDur = { 29000, 1000, 10000, 5000, 2000, 18000 }; //65

        LightColor[] nsL = { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow };
        int[] nsLDur = { 47000, 1000, 10000, 5000, 2000 }; //65

        LightColor[] ew = { LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red };
        int[] ewDur = { 1000, 10000, 5000, 2000, 47000 }; //65

        LightColor[] ewL = { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red };
        int[] ewLDur = { 18000, 1000, 10000, 5000, 2000, 29000 }; //65

        // Gyalogos lámpák
        LightColor[] ewP = { LightColor.Red, LightColor.Green, LightColor.Green_end, LightColor.Red };
        int[] ewPDur = { 30000, 16000, 2000, 17000 }; // 65

        LightColor[] nsP = { LightColor.Green, LightColor.Green_end, LightColor.Red };
        int[] nsPDur = { 16000, 2000, 47000 }; // 65

        int nsIndex = 0;
        int nsLIndex = 0;
        int ewIndex = 0;
        int ewLIndex = 0;
        int ewPIndex = 0;
        int nsPIndex = 0;

        LightColor prevNS = LightColor.OutOfOrder;
        LightColor prevNSL = LightColor.OutOfOrder;
        LightColor prevEW = LightColor.OutOfOrder;
        LightColor prevEWL = LightColor.OutOfOrder;
        LightColor prevEWPed = LightColor.OutOfOrder;
        LightColor prevNSPed = LightColor.OutOfOrder;

        // A következő időpont, amikor az adott lámpa következő fázisra lép
        int nsRemaining = nsDur[nsIndex];
        int nsLRemaining = nsLDur[nsLIndex];
        int ewRemaining = ewDur[ewIndex];
        int ewLRemaining = ewLDur[ewLIndex];
        int ewPRemaining = ewPDur[ewPIndex];
        int nsPRemaining = nsPDur[nsPIndex];

        // Jobbra kanyarodáshoz szükséges változók
        bool nsright = ns[nsIndex] == LightColor.Green || ns[nsIndex] == LightColor.Green_end || ns[nsIndex] == LightColor.Yellow ||
                          ewL[ewLIndex] == LightColor.Green || ewL[ewLIndex] == LightColor.Green_end || ewL[ewLIndex] == LightColor.Yellow;
        bool ewright = ew[ewIndex] == LightColor.Green || ew[ewIndex] == LightColor.Green_end || ew[ewIndex] == LightColor.Yellow ||
                  nsL[nsLIndex] == LightColor.Green || nsL[nsLIndex] == LightColor.Green_end || nsL[nsLIndex] == LightColor.Yellow;

        const int sleepStep = 100;

        // Mentés a visszaállításhoz
        int saved_nsIndex = 0, saved_nsLIndex = 0, saved_ewIndex = 0, saved_ewLIndex = 0, saved_nsPIndex = 0, saved_ewPIndex = 0;
        int saved_nsRemaining = 0, saved_nsLRemaining = 0, saved_ewRemaining = 0, saved_ewLRemaining = 0, saved_nsPRemaining = 0, saved_ewPRemaining = 0;

        while (LampState == States.Running)
        {
            // Gyalogos igény É-D irányban: ha az É-D gyalogos lámpa piros és a fő lámpa is piros
            if (PedestLampState == States.PedestSignedNS &&
                nsP[nsPIndex] == LightColor.Red)
            {
                // Állapotok mentése
                saved_nsIndex = nsIndex; saved_nsLIndex = nsLIndex; saved_ewIndex = ewIndex; saved_ewLIndex = ewLIndex; saved_nsPIndex = nsPIndex; saved_ewPIndex = ewPIndex;

                Console.WriteLine("[DEBUG] Gyalogos igény (É-D): minden lámpa biztonságos állapotba vált, majd visszaáll.");

                // É-D fő zöld, minden más piros, É-D gyalogos zöld, K-Ny gyalogos piros
                nsIndex = 2; // Green
                nsLIndex = 0; // Red
                ewIndex = 4; // Red
                ewLIndex = 0; // Red
                nsPIndex = 0; // Green
                ewPIndex = 0; // Red

                PrintLampColors(ns[nsIndex], nsL[nsLIndex], ew[ewIndex], ewL[ewLIndex], ewP[ewPIndex], nsP[nsPIndex], nsright, ewright);

                // 5 másodpercig ebben az állapotban marad
                int pedGreenTime = 5000;
                while (pedGreenTime > 0 && LampState == States.Running)
                {
                    Thread.Sleep(sleepStep);
                    pedGreenTime -= sleepStep;
                }

                // Gyalogos igény törlése, visszaállítás
                PedestLampState = States.Running;

                // Visszaállítjuk a fázisokat és időzítőket a mentett értékekre
                nsIndex = saved_nsIndex; nsLIndex = saved_nsLIndex; ewIndex = saved_ewIndex; ewLIndex = saved_ewLIndex; nsPIndex = saved_nsPIndex; ewPIndex = saved_ewPIndex;
                continue;
            }

            // Gyalogos igény K-Ny irányban: ha a K-Ny gyalogos lámpa piros és a fő lámpa is piros
            if (PedestLampState == States.PedestSignedEW &&
                ewP[ewPIndex] == LightColor.Red)
            {
                // Állapotok mentése
                saved_nsIndex = nsIndex; saved_nsLIndex = nsLIndex; saved_ewIndex = ewIndex; saved_ewLIndex = ewLIndex; saved_nsPIndex = nsPIndex; saved_ewPIndex = ewPIndex;

                Console.WriteLine("[DEBUG] Gyalogos igény (K-Ny): minden lámpa biztonságos állapotba vált, majd visszaáll.");

                // K-Ny fő zöld, minden más piros, K-Ny gyalogos zöld, É-D gyalogos piros
                ewIndex = 1; // Green
                ewLIndex = 0; // Red
                nsIndex = 0; // Red
                nsLIndex = 0; // Red
                ewPIndex = 1; // Green
                nsPIndex = 2; // Red

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine($"Észak-Dél: {ns[nsIndex]}");
                Console.WriteLine($"Észak-Dél Kanyarodó: {nsL[nsLIndex]}");
                Console.WriteLine($"Kelet-Nyugat: {ew[ewIndex]}");
                Console.WriteLine($"Kelet-Nyugat Kanyarodó: {ewL[ewLIndex]}");
                Console.WriteLine($"Észak-Dél-en keresztül gyalogos : {ewP[ewPIndex]}");
                Console.WriteLine($"Kelet-Nyugaton keresztül gyalogos : {nsP[nsPIndex]}");
                Console.WriteLine("--------------------------------------------------");

                // 5 másodpercig ebben az állapotban marad
                int pedGreenTime = 5000;
                while (pedGreenTime > 0 && LampState == States.Running)
                {
                    Thread.Sleep(sleepStep);
                    pedGreenTime -= sleepStep;
                }

                // Gyalogos igény törlése, visszaállítás
                PedestLampState = States.Running;

                // Visszaállítjuk a fázisokat és időzítőket a mentett értékekre
                nsIndex = saved_nsIndex; nsLIndex = saved_nsLIndex; ewIndex = saved_ewIndex; ewLIndex = saved_ewLIndex; nsPIndex = saved_nsPIndex; ewPIndex = saved_ewPIndex;
                continue;
            }

            // Kiírás csak ha változik
            if (ns[nsIndex] != prevNS || nsL[nsLIndex] != prevNSL || ew[ewIndex] != prevEW || ewL[ewLIndex] != prevEWL || nsP[nsPIndex] != prevNSPed || ewP[ewPIndex] != prevEWPed)
            {

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine($"Észak-Dél: {ns[nsIndex]}");
                prevNS = ns[nsIndex];
                Console.WriteLine($"Észak-Dél Kanyarodó: {nsL[nsLIndex]}");
                prevNSL = nsL[nsLIndex];
                Console.WriteLine($"Kelet-Nyugat: {ew[ewIndex]}");
                prevEW = ew[ewIndex];
                Console.WriteLine($"Kelet-Nyugat Kanyarodó: {ewL[ewLIndex]}");
                prevEWL = ewL[ewLIndex];
                Console.WriteLine($"Észak-Dél-en keresztül gyalogos : {ewP[ewPIndex]}");
                prevEWPed = ewP[ewPIndex];
                Console.WriteLine($"Kelet-Nyugaton keresztül gyalogos : {nsP[nsPIndex]}");
                prevNSPed = nsP[nsPIndex];

                if (nsright)
                    Console.WriteLine("Észak-Dél forgalom kanyarodhat jobbra.");
                if (ewright)
                    Console.WriteLine("Kelet-Nyugat forgalom kanyarodhat jobbra.");
                Console.WriteLine("--------------------------------------------------\n");
            }

            // Alvás egy lépést
            Thread.Sleep(sleepStep);
            if (LampState != States.Running) return;

            // Csökkentjük a hátralévő időket
            nsRemaining -= sleepStep;
            nsLRemaining -= sleepStep;
            ewRemaining -= sleepStep;
            ewLRemaining -= sleepStep;
            nsPRemaining -= sleepStep;
            ewPRemaining -= sleepStep;

            // Amikor lejár egy lámpa fázisa, lépjen a következőre
            if (nsRemaining <= 0)
            {
                nsIndex = (nsIndex + 1) % ns.Length;
                nsRemaining = nsDur[nsIndex];
            }
            if (nsLRemaining <= 0)
            {
                nsLIndex = (nsLIndex + 1) % nsL.Length;
                nsLRemaining = nsLDur[nsLIndex];
            }
            if (ewRemaining <= 0)
            {
                ewIndex = (ewIndex + 1) % ew.Length;
                ewRemaining = ewDur[ewIndex];
            }
            if (ewLRemaining <= 0)
            {
                ewLIndex = (ewLIndex + 1) % ewL.Length;
                ewLRemaining = ewLDur[ewLIndex];
            }
            if (ewPRemaining <= 0)
            {
                ewPIndex = (ewPIndex + 1) % ewP.Length;
                ewPRemaining = ewPDur[ewPIndex];
            }
            if (nsPRemaining <= 0)
            {
                nsPIndex = (nsPIndex + 1) % nsP.Length;
                nsPRemaining = nsPDur[nsPIndex];
            }
        }
    }

    // Üzemen kívüli állapot: sárga villogás
    public static void OutOfOrder()
    {
        while (LampState == States.OutOfOrder)
        {
            Console.WriteLine("A lámpák sárgán villognak 50%-on.");
            Thread.Sleep(2000);
            if (LampState != States.OutOfOrder)
                return;
        }
    }

    // Lámpák és kereszteződés indítása
    public static States Start()
    {
        Console.WriteLine("Kereszteződés Jelzőlámpáinak a szimulációja.");
        Console.WriteLine("Vezérlő gombok:\n\nu: OutOfOrder Állapot\nr: Alap működés visszaállítása\nw:Észak-Déli Irányt keresztező gyalogos jelzés \na:Kelet-Nyugat Irányt keresztező gyalogos jelzés");
        Console.WriteLine("\nLámpák indításához írj 'on' parancsot:");
        string start = Console.ReadLine()?.Trim().ToLower();

        if (string.IsNullOrEmpty(start) || start != "on")
        {
            Console.WriteLine("Lámpák nem lettek bekapcsolva.");
            Environment.Exit(0);
        }

        Console.WriteLine("Kereszteződés indításához írj 'start' parancsot:");
        string enable = Console.ReadLine()?.Trim().ToLower();

        if (enable != "start")
        {
            Console.WriteLine("Üzemen kívül.");
            return States.OutOfOrder;
        }

        return States.Running;
    }

    // Billentyűfigyelő: 'u' -> OutOfOrder, 'r' -> Running, 'w'/'a' -> gyalogos igény
    public static void KeyboardListener()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.U)
                {
                    LampState = States.OutOfOrder;
                }
                else if (key == ConsoleKey.R)
                {
                    LampState = States.Running;
                }
                else if (key == ConsoleKey.W)
                {
                    Console.WriteLine("Az É-D vonalon keresztbe jeleztek a gyalogosok.\n");
                    PedestLampState = States.PedestSignedNS;
                }
                else if (key == ConsoleKey.A)
                {
                    Console.WriteLine("Az K-N vonalon keresztbe jeleztek a gyalogosok.\n");
                    PedestLampState = States.PedestSignedEW;
                }
            }
            Thread.Sleep(100);
        }
    }

    // Program belépési pontja
    static void Main(string[] args)
    {
        LampState = Start();

        // Billentyűfigyelő indítása
        Thread keyListener = new Thread(KeyboardListener);
        keyListener.IsBackground = true;
        keyListener.Start();

        // Fő vezérlési ciklus: állapotnak megfelelő szekvencia futtatása
        while (true)
        {
            switch (LampState)
            {
                case States.Running:
                    LampSequence();
                    break;
                case States.OutOfOrder:
                    OutOfOrder();
                    break;
            }
        }
    }
}
