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
    OutOfOrder
}

class Program
{
    // Aktuális állapot, amit a billentyűfigyelő módosíthat
    static volatile States currentState;

    // Jelzőlámpa vezérlési szekvencia
    public static void LampSequence()
    {
        // Észak-Dél irány szekvenciája és időtartamai
        LightColor[] nsSequence = {
            LightColor.Red,
            LightColor.Red_Yellow,
            LightColor.Green,
            LightColor.Green_end,
            LightColor.Yellow
        };
        int[] nsDurations = { 17000, 1000, 10000, 5000, 2000 };

        // Kelet-Nyugat irány zöld fázisai és időtartamai
        LightColor[] ewGreenPhase = {
            LightColor.Green,
            LightColor.Green_end,
            LightColor.Yellow
        };
        int[] ewDurations = { 10000, 5000, 2000 };

        int i = 0;

        // Fő vezérlési ciklus, amíg fut a rendszer
        while (currentState == States.Running)
        {
            var nsState = nsSequence[i];
            int nsDuration = nsDurations[i];

            // Ha É-D irány piros, K-Ny irány zöld fázisokat futtat
            if (nsState == LightColor.Red)
            {
                for (int j = 0; j < ewGreenPhase.Length; j++)
                {
                    if (currentState != States.Running) return;

                    Console.WriteLine($"É-D irány: {nsState}");
                    Console.WriteLine($"K-Ny irány: {ewGreenPhase[j]}");

                    Thread.Sleep(ewDurations[j]);
                }
            }
            else
            {
                // Egyébként K-Ny irány piros, É-D irány szekvencia
                Console.WriteLine($"É-D irány: {nsState}");
                Console.WriteLine($"K-Ny irány: Red");

                int slept = 0;
                while (slept < nsDuration)
                {
                    if (currentState != States.Running) return;
                    Thread.Sleep(100);
                    slept += 100;
                }
            }

            i = (i + 1) % nsSequence.Length;
        }
    }

    // Üzemen kívüli állapot: sárga villogás
    public static void OutOfOrder()
    {
        while (currentState == States.OutOfOrder)
        {
            Console.WriteLine("A lámpa sárgán villog 50%-on.");
            Thread.Sleep(2000);
            if (currentState != States.OutOfOrder)
                return;
        }
    }

    // Indítási logika: bekapcsolás és start parancs bekérése
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

        // Billentyűfigyelő indítása háttérszálon
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
