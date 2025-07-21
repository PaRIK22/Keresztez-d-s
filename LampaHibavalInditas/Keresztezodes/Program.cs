using System;
using System.IO;
using System.Threading;
using System.Timers;

public enum LightColor
{
    Red,     // Amíg veszélyes -> most 10s
    Red_Yellow,  // 1s
    Green,    // 10s
    Green_end, // 5s
    Yellow, // 2s
    OutOfOrder //Sárga villog, kézi újraindítás
}

public enum States
{
    Off, // Kikapcsolt állapot
    Running, // Folyamatban van a vezérlés
    OutOfOrder // Üzemen kívül
}


class Program
{
    public static void LampSequence()
    {
        LightColor currentColor = LightColor.Red; // Kezdő szín
        while (true)
        {

            switch (currentColor)
            {
                case LightColor.Red:
                    currentColor = LightColor.Red; // Visszatérés a pirosra
                    Console.WriteLine($"A lámpa pirosan világít.");
                    System.Threading.Thread.Sleep(5000); ; // 5 másodperc
                    currentColor = LightColor.Red_Yellow;
                    break;
                case LightColor.Red_Yellow:
                    currentColor = LightColor.Red_Yellow;
                    Console.WriteLine($"A lámpa pirosan és sárgán világít.");
                    System.Threading.Thread.Sleep(1000); // 1 másodperc
                    currentColor = LightColor.Green;
                    break;
                case LightColor.Green:
                    currentColor = LightColor.Green;
                    Console.WriteLine($"A lámpa színe zölden világít.");
                    System.Threading.Thread.Sleep(5000); ; // 10 másodperc
                    currentColor = LightColor.Green_end;
                    break;
                case LightColor.Green_end:
                    currentColor = LightColor.Green_end;
                    Console.WriteLine($"A lámpa zölden villog.");
                    System.Threading.Thread.Sleep(5000); // 5 másodperc
                    currentColor = LightColor.Yellow;
                    break;
                case LightColor.Yellow:
                    currentColor = LightColor.Yellow;
                    Console.WriteLine($"A lámpa sárgán világít.");
                    System.Threading.Thread.Sleep(2000); ; // 2 másodperc
                    currentColor = LightColor.Red;
                    break;
            }
        }
    }
    public static States Start()
    {
        States currentState;
        Console.WriteLine("Lámpák indításához írj 'on' parancsot:");
        string start = Console.ReadLine()?.Trim().ToLower(); 

        if (string.IsNullOrEmpty(start) || start != "on")
        {
            Console.WriteLine("Lámpák nem lettek bekapcsolva.");
            return default;
        }
        else
        {
            Console.WriteLine("Kereszteződés indításához írj 'start' parancsot:");
            string enable = Console.ReadLine()?.Trim().ToLower();
            if (string.IsNullOrEmpty(enable) || enable != "start")
            {
                Console.WriteLine("Üzemen kívül.");
                return States.Off;
            }
            else
            {
                return States.Running;
            }
        }
    }


    public static void OutOfOrder(States CurrentState)
    {
        while(CurrentState == States.OutOfOrder)
        {
            LightColor currentColor = LightColor.Yellow;
            Console.WriteLine("A lámpa sárgán villog 50%-on.");
            System.Threading.Thread.Sleep(2000);
        }

    }

    static void Main(string[] args)
    {
        States currentState = Start();

        switch (currentState)
        {
            case States.Running:
                Console.WriteLine("Kereszteződés vezérlés elindítva.");
                LampSequence();
                break;
            case States.Off:
                currentState = States.Off;
                OutOfOrder(currentState);
                break;
            default:
                Console.WriteLine("A lámpa üzemen kívül van.");
                break;
        }
    }
}