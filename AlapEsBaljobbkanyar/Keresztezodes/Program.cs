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
    Red_l,
    Red_Yellow_l,
    Green_l,
    Green_end_l,
    Yellow_l,
    OutOfOrder
}

// Rendszer állapotok felsorolása
public enum States
{
    Running,
    OutOfOrder
}

class Program
{
    // Aktuális állapot, amit a billentyűfigyelő módosíthat
    static volatile States currentState;

    // Jelzőlámpa vezérlési szekvencia
    public static void LampSequence()
    {
        // Jobbra kanyarodáshoz szükséges változók
        bool nsright;
        bool ewright;

        LightColor[] ns = { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red };
        int[] nsDur = { 29000, 1000, 10000, 5000, 2000, 18000 };

        LightColor[] nsL = { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow };
        int[] nsLDur = { 47000, 1000, 5000, 3000, 2000};

        LightColor[] ew = { LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red };
        int[] ewDur = { 1000, 10000, 5000, 2000, 47000 };

        LightColor[] ewL = { LightColor.Red, LightColor.Red_Yellow, LightColor.Green, LightColor.Green_end, LightColor.Yellow, LightColor.Red };
        int[] ewLDur = { 18000, 1000, 5000, 3000, 2000, 29000 };

        int nsIndex = 0;
        int nsLIndex = 0;
        int ewIndex = 0;
        int ewLIndex = 0;

        LightColor prevNS = LightColor.OutOfOrder;
        LightColor prevNSL = LightColor.OutOfOrder;
        LightColor prevEW = LightColor.OutOfOrder;
        LightColor prevEWL = LightColor.OutOfOrder;

        // A következő időpont, amikor az adott lámpa következő fázisra lép
        int nsRemaining = nsDur[nsIndex];
        int nsLRemaining = nsLDur[nsLIndex];
        int ewRemaining = ewDur[ewIndex];
        int ewLRemaining = ewLDur[ewLIndex];

        const int sleepStep = 100;

        while (currentState == States.Running)
        {
            // Kiírás csak ha változik
            if (ns[nsIndex] != prevNS || nsL[nsLIndex] != prevNSL || ew[ewIndex] != prevEW || ewL[ewLIndex] != prevEWL)
            {
               
                if (ns[nsIndex] == LightColor.Green || ns[nsIndex] == LightColor.Green_end || ns[nsIndex] == LightColor.Yellow || ewL[ewLIndex] == LightColor.Green || ewL[ewLIndex] == LightColor.Green_end || ewL[ewLIndex] == LightColor.Yellow)
                {
                    nsright = true;
                }
                else
                {
                    nsright = false;
                }

                if(ew[ewIndex] == LightColor.Green || ew[ewIndex] == LightColor.Green_end || ew[ewIndex] == LightColor.Yellow || nsL[nsLIndex] == LightColor.Green || nsL[nsLIndex] == LightColor.Green_end || nsL[nsLIndex] == LightColor.Yellow)
                {
                    ewright = true;
                }
                else
                {
                    ewright = false;
                }

                Console.WriteLine($"Észak-Dél: {ns[nsIndex]}");
                prevNS = ns[nsIndex];
                Console.WriteLine($"Észak-Dél Kanyarodó: {nsL[nsLIndex]}");
                prevNSL = nsL[nsLIndex];
                Console.WriteLine($"Kelet-Nyugat: {ew[ewIndex]}");
                prevEW = ew[ewIndex];
                Console.WriteLine($"Kelet-Nyugat Kanyarodó: {ewL[ewLIndex]}");
                prevEWL = ewL[ewLIndex];
                if (nsright)
                {
                    Console.WriteLine("Észak-Dél forgalom kanyarodhat jobbra.");
                }
                if (ewright)
                {
                    Console.WriteLine("Kelet-Nyugat forgalom kanyarodhat jobbra.");
                }
                Console.WriteLine("--------------------------------------------------\n");
            }
          

            // Alvás egy lépést
            Thread.Sleep(sleepStep);
            if (currentState != States.Running) return;

            // Csökkentjük a hátralévő időket
            nsRemaining -= sleepStep;
            nsLRemaining -= sleepStep;
            ewRemaining -= sleepStep;
            ewLRemaining -= sleepStep;

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
        }
    }

    // Üzemen kívüli állapot: sárga villogás
    public static void OutOfOrder()
    {
        while (currentState == States.OutOfOrder)
        {
            Console.WriteLine("A lámpák sárgán villognak 50%-on.");
            Thread.Sleep(2000);
            if (currentState != States.OutOfOrder)
                return;
        }
    }

    // Lámpák és kereszteződés indítása
    public static States Start()
    {
        Console.WriteLine("Lámpák indításához írj 'on' parancsot:");
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

    // Billentyűfigyelő: 'u' -> OutOfOrder, 'r' -> Running
    public static void KeyboardListener()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.U)
                {
                    currentState = States.OutOfOrder;
                }
                else if (key == ConsoleKey.R)
                {
                    currentState = States.Running;
                }
            }
            Thread.Sleep(100);
        }
    }

    // Program belépési pontja
    static void Main(string[] args)
    {
        currentState = Start();

        // Billentyűfigyelő indítása
        Thread keyListener = new Thread(KeyboardListener);
        keyListener.IsBackground = true;
        keyListener.Start();

        // Fő vezérlési ciklus: állapotnak megfelelő szekvencia futtatása
        while (true)
        {
            switch (currentState)
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
