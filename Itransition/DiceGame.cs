using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class DiceGame
{
    static RandomNumberGenerator rng = RandomNumberGenerator.Create();

    static byte[] GetRandomBytes(int length)
    {
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return bytes;
    }

    static string HMAC_SHA256(byte[] key, byte[] message) =>
        BitConverter.ToString(new HMACSHA256(key).ComputeHash(message)).Replace("-", "");

    static int GetSecureRandom(int max)
    {
        var buffer = new byte[4];
        int value;
        do
        {
            rng.GetBytes(buffer);
            value = BitConverter.ToInt32(buffer, 0) & int.MaxValue;
        } while (value >= max * (int.MaxValue / max));
        return value % max;
    }

    static (int result, byte[] key, string hmac) FairRoll(int max)
    {
        byte[] key = GetRandomBytes(32);
        int compNum = GetSecureRandom(max);
        string hmac = HMAC_SHA256(key, BitConverter.GetBytes(compNum));
        return (compNum, key, hmac);
    }

    static int GetUserInput(string prompt, string[] options)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            for (int i = 0; i < options.Length; i++)
                Console.WriteLine($"{i} - {options[i]}");
            Console.WriteLine("X - exit\n? - help");
            Console.Write("Your selection: ");
            var input = Console.ReadLine()?.Trim().ToUpper();

            if (input == "X") Environment.Exit(0);
            if (input == "?")
            {
                Console.WriteLine("Help: Choose one of the listed options.");
                continue;
            }

            if (int.TryParse(input, out int choice) && choice >= 0 && choice < options.Length)
                return choice;

            Console.WriteLine("Invalid input, try again.");
        }
    }

    static int FairDiceRoll(int compNum, byte[] key, int userAdd, int mod, int[] dice)
    {
        Console.WriteLine($"My number is {compNum} (KEY={BitConverter.ToString(key).Replace("-", "")}).");
        int result = (compNum + userAdd) % mod;
        Console.WriteLine($"The fair number generation result is {compNum} + {userAdd} = {result} (mod {mod}).");
        Console.WriteLine($"Roll result is {dice[result]}.");
        return dice[result];
    }

    static void Main()
    {
        var diceSets = new[]
        {
             new[] {1,2,3,4,5,6},
             new[] {1,2,3,4,5,6},
             new[] {1,2,3,4,5,6},
             new[] {1,2,3,4,5,6},
             new[] {2,2,4,4,9,9},
             new[] {1,1,6,6,8,8},
             new[] {3,3,5,5,7,7}
        };

        Console.WriteLine("Let's determine who makes the first move.");
        var (firstRand, firstKey, firstHmac) = FairRoll(2);
        Console.WriteLine($"I selected a random value in the range 0..1 (HMAC={firstHmac}).");

        int userFirstGuess = GetUserInput("", new[] { "0", "1" });
        Console.WriteLine($"My selection: {firstRand} (KEY={BitConverter.ToString(firstKey).Replace("-", "")}).");

        bool computerStarts = firstRand != userFirstGuess;
        int compDiceIdx = computerStarts ? 1 : GetUserInput("Choose your dice:", diceSets.Select(d => string.Join(",", d)).Where((v, i) => i != 1).ToArray());
        int userDiceIdx = 3 - compDiceIdx - 1; 

        var compDice = diceSets[compDiceIdx];
        var userDice = diceSets[userDiceIdx];

        Console.WriteLine($"{(computerStarts ? "I" : "You")} make the first move and choose the [{string.Join(",", compDice)}] dice.");
        Console.WriteLine($"{(computerStarts ? "You" : "I")} choose the [{string.Join(",", userDice)}] dice.");

        
        Console.WriteLine("It's time for my roll.");
        var (compNum, compKey, compHmac) = FairRoll(6);
        Console.WriteLine($"I selected a random value in the range 0..5 (HMAC={compHmac}).");
        int userAdd1 = GetUserInput("Add your number modulo 6.", Enumerable.Range(0, 6).Select(i => i.ToString()).ToArray());
        int compRoll = FairDiceRoll(compNum, compKey, userAdd1, 6, compDice);

        
        Console.WriteLine("It's time for your roll.");
        var (userNum, userKey, userHmac) = FairRoll(6);
        Console.WriteLine($"I selected a random value in the range 0..5 (HMAC={userHmac}).");
        int userAdd2 = GetUserInput("Add your number modulo 6.", Enumerable.Range(0, 6).Select(i => i.ToString()).ToArray());
        int userRoll = FairDiceRoll(userNum, userKey, userAdd2, 6, userDice);

        
        Console.WriteLine(userRoll > compRoll
            ? $"You win ({userRoll} > {compRoll})!"
            : userRoll < compRoll
                ? $"I win ({compRoll} > {userRoll})!"
                : $"It's a tie!");
    }
}
