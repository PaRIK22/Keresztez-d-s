using System;
using System.Threading;

public enum LightColor
{
    Red,
    Red_Yellow,
    Green,
    Green_end,
    Yellow,
    OutOfOrder
}

public enum States
{
    Running,
    OutOfOrder
}

class Program
{
    static volatile States currentState;

    public static void LampSequence()
    {
        LightColor[] sequence = {
            LightColor.Red,
            LightColor.Red_Yellow,
            LightColor.Green,
            LightColor.Green_end,
            LightColor.Yellow
        };

        int[] durations = { 5000, 1000, 5000, 5000, 2000 };

        int i = 0;
        while (currentState == States.Running)
        {
            Console.WriteLine($"A lámpa állapota: {sequence[i]}");
            int sleepInterval = durations[i];
            for (int j = 0; j < sleepInterval / 100; j++)
            {
                if (currentState != States.Running)
                    return; // azonnali kilépés, ha állapot változott
                Thread.Sleep(100); // rövid szakaszokban alszik
            }

            i = (i + 1) % sequence.Length; // ciklikusan ismétli
        }
    }

    public static void OutOfOrder()
    {
        bool on = true;
        while (currentState == States.OutOfOrder)
        {
        LightColor currentColor = LightColor.Yellow;
        Console.WriteLine("A lámpa sárgán villog 50%-on.");
        System.Threading.Thread.Sleep(2000);
        if (currentState != States.OutOfOrder)
            return;

        }
    }

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

    static void Main(string[] args)
    {
        currentState = Start();

        Thread keyListener = new Thread(KeyboardListener);
        keyListener.IsBackground = true;
        keyListener.Start();

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
                default:
                    
                    break;
            }
        }
    }
}
