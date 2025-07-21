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
        Console.WriteLine("Lámpák indításához írj 'start' parancsot:");
        string start = Console.ReadLine();
        if (string.IsNullOrEmpty(start) || start.ToLower() != "start")
        {
             Console.WriteLine("Lámpák nem lettek bekapcsolva.");
            return default;
        }
        else
        {
            Console.WriteLine("Kereszteződés indításához írj 'start' parancsot:");
            string enable = Console.ReadLine();
            if (string.IsNullOrEmpty(enable) || enable.ToLower() != "start")
            {
                return States.Off;
            }
            else
            {
                return States.Running;
            }
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
                break;
            default:
                Console.WriteLine("Usage: Keresztezodes start|stop");
                break;
        }
    }
}