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

// Lámpafázis osztály (1. pont)
public class LampPhase
{
    public LightColor[] Sequence { get; }
    public int[] Durations { get; }
    public int Index { get; set; }
    public int Remaining { get; set; }

    public LampPhase(LightColor[] sequence, int[] durations)
    {
        Sequence = sequence;
        Durations = durations;
        Index = 0;
        Remaining = durations[0];
    }

    public void Advance()
    {
        Index = (Index + 1) % Sequence.Length;
        Remaining = Durations[Index];
    }

    public LightColor CurrentColor => Sequence[Index];
}

class Program
{
    static volatile States LampState;
    static volatile States PedestLampState;

    // Lámpák aktuális állapotának megjelenítése
    static void PrintLampColors(
        LampPhase ns, LampPhase nsL, LampPhase ew, LampPhase ewL, LampPhase ewP, LampPhase nsP)
    {
        bool nsright = ns.CurrentColor == LightColor.Green || ns.CurrentColor == LightColor.Green_end || ns.CurrentColor == LightColor.Yellow ||
                       ewL.CurrentColor == LightColor.Green || ewL.CurrentColor == LightColor.Green_end || ewL.CurrentColor == LightColor.Yellow;
        bool ewright = ew.CurrentColor == LightColor.Green || ew.CurrentColor == LightColor.Green_end || ew.CurrentColor == LightColor.Yellow ||
                       nsL.CurrentColor == LightColor.Green || nsL.CurrentColor == LightColor.Green_end || nsL.CurrentColor == LightColor.Yellow;

        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"Észak-Dél: {ns.CurrentColor}");
        Console.WriteLine($"Észak-Dél Kanyarodó: {nsL.CurrentColor}");
        Console.WriteLine($"Kelet-Nyugat: {ew.CurrentColor}");
        Console.WriteLine($"Kelet-Nyugat Kanyarodó: {ewL.CurrentColor}");
        Console.WriteLine($"Észak-Dél irányba gyalogos : {nsP.CurrentColor}");
        Console.WriteLine($"Kelet-Nyugat irányba gyalogos : {ewP.CurrentColor}");
        if (nsright)
            Console.WriteLine("Észak-Dél forgalom kanyarodhat jobbra.");
        if (ewright)
            Console.WriteLine("Kelet-Nyugat forgalom kanyarodhat jobbra.");
        Console.WriteLine("--------------------------------------------------");
    }

    public static void LampSequence()
    {
        // Lámpafázisok példányosítása LampPhase osztállyal
        var ns = new LampPhase(
            new[] { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red },
            new[] { 36000, 1000, 10000, 5000, 2000, 18000 }
        );
        var nsL = new LampPhase(
            new[] { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow },
            new[] { 54000, 1000, 10000, 5000, 2000 }
        );
        var ew = new LampPhase(
            new[] { LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red },
            new[] { 1000, 10000, 5000, 2000, 54000 }
        );
        var ewL = new LampPhase(
            new[] { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red },
            new[] { 18000, 1000, 10000, 5000, 2000, 36000 }
        );
        var nsP = new LampPhase(
            new[] { LightColor.Red, LightColor.Green, LightColor.Green_end, LightColor.Red },
            new[] { 36000, 16000, 2000, 18000 }
        );
        var ewP = new LampPhase(
            new[] { LightColor.Green, LightColor.Green_end, LightColor.Red },
            new[] { 16000, 2000, 54000 }
        );

        // Előző állapotok a változás figyeléséhez
        LightColor prevNS = LightColor.OutOfOrder;
        LightColor prevNSL = LightColor.OutOfOrder;
        LightColor prevEW = LightColor.OutOfOrder;
        LightColor prevEWL = LightColor.OutOfOrder;
        LightColor prevEWPed = LightColor.OutOfOrder;
        LightColor prevNSPed = LightColor.OutOfOrder;

        const int sleepStep = 100;

        // Mentés a visszaállításhoz
        int saved_nsIndex = 0, saved_nsLIndex = 0, saved_ewIndex = 0, saved_ewLIndex = 0, saved_nsPIndex = 0, saved_ewPIndex = 0;

        while (LampState == States.Running)
        {
            // Gyalogos igény É-D irányban
            if (PedestLampState == States.PedestSignedNS &&
                nsP.CurrentColor == LightColor.Red)
            {
                saved_nsIndex = ns.Index; saved_nsLIndex = nsL.Index; saved_ewIndex = ew.Index; saved_ewLIndex = ewL.Index; saved_nsPIndex = nsP.Index; saved_ewPIndex = ewP.Index;

                if (ns.Index == 0 || ns.Index == 5)
                {
                    ns.Index = 1; // Red_Yellow
                }
                else
                {
                    ns.Index = 2; // Green
                }

                if (nsL.Index != 0)
                {
                    nsL.Index = 4; // Yellow
                }

                if (ew.Index != 4)
                {
                    ew.Index = 3; // Yellow
                }

                if (ewL.Index != 0 || ewL.Index != 5)
                {
                    ewL.Index = 4; // Yellow
                }

                nsP.Index = 1; // Green
                ewP.Index = 2; // Red

                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);

                int pedGreenTime = 1000;
                while (pedGreenTime > 0 && LampState == States.Running)
                {
                    Thread.Sleep(sleepStep);
                    pedGreenTime -= sleepStep;
                }

                ns.Index = 2; // Green
                nsL.Index = 0; // Red
                ew.Index = 4; // Red
                ewL.Index = 0; // Red

                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);

                pedGreenTime = 4000;
                while (pedGreenTime > 0 && LampState == States.Running)
                {
                    Thread.Sleep(sleepStep);
                    pedGreenTime -= sleepStep;
                }

                PedestLampState = States.Running;

                ns.Index = saved_nsIndex; nsL.Index = saved_nsLIndex; ew.Index = saved_ewIndex; ewL.Index = saved_ewLIndex; nsP.Index = saved_nsPIndex; ewP.Index = saved_ewPIndex;
                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);
                continue;
            }

            // Gyalogos igény K-Ny irányban
            if (PedestLampState == States.PedestSignedEW &&
                ewP.CurrentColor == LightColor.Red)
            {
                saved_nsIndex = ns.Index; saved_nsLIndex = nsL.Index; saved_ewIndex = ew.Index; saved_ewLIndex = ewL.Index; saved_nsPIndex = nsP.Index; saved_ewPIndex = ewP.Index;

                if (ns.Index != 0 || ns.Index != 5)
                {
                    ns.Index = 4; // Yellow
                }

                if (nsL.Index != 0)
                {
                    nsL.Index = 4; // Yellow
                }

                if (ew.Index == 4)
                {
                    ew.Index = 0; // Red_Yellow
                }
                else
                {
                    ew.Index = 1; // Green
                }

                if (ewL.Index != 0 || ewL.Index != 5)
                {
                    ewL.Index = 4; // Yellow
                }

                nsP.Index = 0; // Red
                ewP.Index = 0; // Green

                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);

                int pedGreenTime = 1000;
                while (pedGreenTime > 0 && LampState == States.Running)
                {
                    Thread.Sleep(sleepStep);
                    pedGreenTime -= sleepStep;
                }

                ns.Index = 0; // Red
                nsL.Index = 0; // Red
                ew.Index = 1; // Green
                ewL.Index = 0; // Red

                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);

                pedGreenTime = 4000;
                while (pedGreenTime > 0 && LampState == States.Running)
                {
                    Thread.Sleep(sleepStep);
                    pedGreenTime -= sleepStep;
                }

                PedestLampState = States.Running;

                ns.Index = saved_nsIndex; nsL.Index = saved_nsLIndex; ew.Index = saved_ewIndex; ewL.Index = saved_ewLIndex; nsP.Index = saved_nsPIndex; ewP.Index = saved_ewPIndex;
                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);

                continue;
            }

            // Kiírás csak ha változik
            if (ns.CurrentColor != prevNS || nsL.CurrentColor != prevNSL || ew.CurrentColor != prevEW ||
                ewL.CurrentColor != prevEWL || nsP.CurrentColor != prevNSPed || ewP.CurrentColor != prevEWPed)
            {
                PrintLampColors(ns, nsL, ew, ewL, ewP, nsP);
                prevNS = ns.CurrentColor;
                prevNSL = nsL.CurrentColor;
                prevEW = ew.CurrentColor;
                prevEWL = ewL.CurrentColor;
                prevEWPed = ewP.CurrentColor;
                prevNSPed = nsP.CurrentColor;
            }

            Thread.Sleep(sleepStep);
            if (LampState != States.Running) return;

            ns.Remaining -= sleepStep;
            nsL.Remaining -= sleepStep;
            ew.Remaining -= sleepStep;
            ewL.Remaining -= sleepStep;
            nsP.Remaining -= sleepStep;
            ewP.Remaining -= sleepStep;

            if (ns.Remaining <= 0) ns.Advance();
            if (nsL.Remaining <= 0) nsL.Advance();
            if (ew.Remaining <= 0) ew.Advance();
            if (ewL.Remaining <= 0) ewL.Advance();
            if (nsP.Remaining <= 0) nsP.Advance();
            if (ewP.Remaining <= 0) ewP.Advance();
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
                    Console.WriteLine("Az É-D irányba jeleztek a gyalogosok.\n");
                    PedestLampState = States.PedestSignedNS;
                }
                else if (key == ConsoleKey.A)
                {
                    Console.WriteLine("Az K-N irányba jeleztek a gyalogosok.\n");
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
