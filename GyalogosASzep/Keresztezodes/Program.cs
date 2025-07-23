using System;
using System.Collections.Generic;
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

// Lámpafázis osztály
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

    // Lámpák aktuális állapotának megjelenítése - most már listával
    static void PrintLampColors(IList<LampPhase> lamps)
    {
        // Sorrend: NS, NSL, EW, EWL, EWP, NSP
        var ns = lamps[0];
        var nsL = lamps[1];
        var ew = lamps[2];
        var ewL = lamps[3];
        var ewP = lamps[4];
        var nsP = lamps[5];

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

    // Gyalogos igények kezelése külön metódusban
    static bool HandlePedestrianRequest(Dictionary<string, LampPhase> phases, ref States pedestLampState, int sleepStep)
    {
        var savedIndexes = new Dictionary<string, int>();
        var lampList = new List<LampPhase> { phases["NS"], phases["NSL"], phases["EW"], phases["EWL"], phases["EWP"], phases["NSP"] };

        if (pedestLampState == States.PedestSignedNS && phases["NSP"].CurrentColor == LightColor.Red)
        {
            foreach (var key in phases.Keys)
                savedIndexes[key] = phases[key].Index;

            // Fázisok beállítása a gyalogos igényhez
            phases["NS"].Index = (phases["NS"].Index == 0 || phases["NS"].Index == 5) ? 1 : 2; //Red -> Red_Yellow Vagy Green
            if (phases["NSL"].Index != 0) phases["NSL"].Index = 4; //Nem Red -> Yellow
            if (phases["EW"].Index != 4) phases["EW"].Index = 3; //Nem Red -> Yellow
            if (phases["EWL"].Index != 0 && phases["EWL"].Index != 5) phases["EWL"].Index = 4; //Nem Red -> Yellow
            phases["NSP"].Index = 1; // Green
            phases["EWP"].Index = 2; // Red

            PrintLampColors(lampList);

            int pedGreenTime = 1000;
            while (pedGreenTime > 0 && LampState == States.Running)
            {
                Thread.Sleep(sleepStep);
                pedGreenTime -= sleepStep;
            }

            phases["NS"].Index = 2; // Green
            phases["NSL"].Index = 0; // Red
            phases["EW"].Index = 4; // Red
            phases["EWL"].Index = 0; // Red

            PrintLampColors(lampList);

            pedGreenTime = 4000;
            while (pedGreenTime > 0 && LampState == States.Running)
            {
                Thread.Sleep(sleepStep);
                pedGreenTime -= sleepStep;
            }

            pedestLampState = States.Running;

            foreach (var key in phases.Keys)
                phases[key].Index = savedIndexes[key];

            PrintLampColors(lampList);
            return true;
        }

        if (pedestLampState == States.PedestSignedEW && phases["EWP"].CurrentColor == LightColor.Red)
        {
            foreach (var key in phases.Keys)
                savedIndexes[key] = phases[key].Index;

            // Fázisok beállítása a gyalogos igényhez
            if (phases["NS"].Index != 0 && phases["NS"].Index != 5) phases["NS"].Index = 4; // Nem Red -> Yellow
            if (phases["NSL"].Index != 0) phases["NSL"].Index = 4; // Nem Red -> Yellow
            phases["EW"].Index = (phases["EW"].Index == 4) ? 0 : 1; //Red -> Red_Yellow Vagy Green
            if (phases["EWL"].Index != 0 && phases["EWL"].Index != 5) phases["EWL"].Index = 4; // Nem Red -> Yellow 
            phases["NSP"].Index = 0; // Red
            phases["EWP"].Index = 0; // Green

            PrintLampColors(lampList);

            int pedGreenTime = 1000;
            while (pedGreenTime > 0 && LampState == States.Running)
            {
                Thread.Sleep(sleepStep);
                pedGreenTime -= sleepStep;
            }

            phases["NS"].Index = 0; // Red
            phases["NSL"].Index = 0; // Red
            phases["EW"].Index = 1; // Green
            phases["EWL"].Index = 0; // Red

            PrintLampColors(lampList);

            pedGreenTime = 4000;
            while (pedGreenTime > 0 && LampState == States.Running)
            {
                Thread.Sleep(sleepStep);
                pedGreenTime -= sleepStep;
            }

            pedestLampState = States.Running;

            foreach (var key in phases.Keys)
                phases[key].Index = savedIndexes[key];

            PrintLampColors(lampList);
            return true;
        }
        return false;
    }

    public static void LampSequence()
    {
        var phases = new Dictionary<string, LampPhase>
        {
            ["NS"] = new LampPhase(
                new[] { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red },
                new[] { 36000, 1000, 10000, 5000, 2000, 18000 }
            ),
            ["NSL"] = new LampPhase(
                new[] { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow },
                new[] { 54000, 1000, 10000, 5000, 2000 }
            ),
            ["EW"] = new LampPhase(
                new[] { LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red },
                new[] { 1000, 10000, 5000, 2000, 54000 }
            ),
            ["EWL"] = new LampPhase(
                new[] { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red },
                new[] { 18000, 1000, 10000, 5000, 2000, 36000 }
            ),
            ["NSP"] = new LampPhase(
                new[] { LightColor.Red, LightColor.Green, LightColor.Green_end, LightColor.Red },
                new[] { 36000, 16000, 2000, 18000 }
            ),
            ["EWP"] = new LampPhase(
                new[] { LightColor.Green, LightColor.Green_end, LightColor.Red },
                new[] { 16000, 2000, 54000 }
            )
        };

        var lampList = new List<LampPhase> { phases["NS"], phases["NSL"], phases["EW"], phases["EWL"], phases["EWP"], phases["NSP"] };

        var prevState = new List<LightColor>
        {
            LightColor.OutOfOrder, // NS
            LightColor.OutOfOrder, // NSL
            LightColor.OutOfOrder, // EW
            LightColor.OutOfOrder, // EWL
            LightColor.OutOfOrder, // EWP
            LightColor.OutOfOrder  // NSP
        };

        const int sleepStep = 100;

        while (LampState == States.Running)
        {
            // Gyalogos igények kezelése külön metódusban
            if (HandlePedestrianRequest(phases, ref PedestLampState, sleepStep))
                continue;

            // Kiírás csak ha változik
            bool changed = false;
            for (int i = 0; i < lampList.Count; i++)
            {
                if (lampList[i].CurrentColor != prevState[i])
                {
                    changed = true;
                    break;
                }
            }
            if (changed)
            {
                PrintLampColors(lampList);
                for (int i = 0; i < lampList.Count; i++)
                    prevState[i] = lampList[i].CurrentColor;
            }

            Thread.Sleep(sleepStep);
            if (LampState != States.Running) return;

            foreach (var phase in lampList)
                phase.Remaining -= sleepStep;

            foreach (var phase in lampList)
                if (phase.Remaining <= 0)
                    phase.Advance();
        }
    }

    // Üzemzavar állapot kezelése
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

    // Szimuláció indítása
    public static States Start()
    {
        //Alap irányításhoz szükséges infók
        Console.WriteLine("Kereszteződés Jelzőlámpáinak a szimulációja.");
        Console.WriteLine("Vezérlő gombok:\n\nu: OutOfOrder Állapot\nr: Alap működés visszaállítása\nw:Észak-Déli Irányt keresztező gyalogos jelzés \na:Kelet-Nyugat Irányt keresztező gyalogos jelzés");
        
        //Indítás módjának meghatározása
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

    // Gombok figyelése a háttérben
    public static void KeyboardListener()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                // A gombok kezelése
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

    static void Main(string[] args)
    {
        // Alapértelmezett állapot beállítása
        LampState = Start();

        //Gombok figyelésének indítása
        Thread keyListener = new Thread(KeyboardListener);
        keyListener.IsBackground = true;
        keyListener.Start();

        // Fő ciklus, amely a lámpák működését kezeli
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
