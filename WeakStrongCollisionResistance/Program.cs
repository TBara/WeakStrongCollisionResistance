using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace WeakStrongCollisionResistance
{
    class Program
    {
        static void Main(string[] args)
        {
            Collisions collisions = new Collisions();

            Thread WeakCollisionsCaller = new Thread(
                new ThreadStart(collisions.WeakCollision));
            WeakCollisionsCaller.Start();

            Thread StrongCollisionCaller = new Thread(
                new ThreadStart(collisions.StrongCollision));
            StrongCollisionCaller.Start();
        }
    }



    public class Collisions
    {
        // Builds a random string and gets the first 24 bits of its hash. 
        // Then it does the same until it finds a string with the same first 24 bits of the hash.
        public void WeakCollision()
        {
            // Create a log and start the timer
            List<string> log = new List<string>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            double accumulator = 0;
            int attempt_cnt = 0;
            do
            {
                // Build the first string and hash value. This is what we're trying to match
                string phrase = BuildString2();
                string phrase_bytes_str = GetFirst24bits(phrase);
                log.Add($"Attempting to match chars: {phrase} with hash value: {phrase_bytes_str}");

                double counter = 0;
                bool match_found = false;
                do
                {
                    // Build a random string and see if its first 24 bits match with the first 24 bits of the first string.
                    string input_str = BuildString();
                    string rand_str = GetFirst24bits(input_str);
                    if (phrase_bytes_str == rand_str)
                    {
                        match_found = true;
                        log.Add($" Phrase {phrase_bytes_str} with hash value {input_str} matched on {counter.ToString()} th try.\n"); // Log the results
                    }
                    counter++;
                } while ((!match_found) && (counter < 35000000)); // Try up to 35 million random combinations. Move on if no match found.
                accumulator += counter;

                attempt_cnt++;
            } while (attempt_cnt < 5);
            sw.Stop();
            string elapsed = sw.Elapsed.ToString();
            log.Add($"\nIt took average of {(accumulator / attempt_cnt).ToString()} trys to find a match\nElapsed time: {elapsed}");
            PrintLog(log);
        }

        // Attempts to match 24 bits of newll generated hash to existing values in a dictionary. 
        // If match is found, message is logged and new search is initiated. 
        // If no match is found, a new hash value is generated and searched again in the dictionary
        public void StrongCollision()
        {
            List<string> log = new List<string>();
            Dictionary<string, string> cipher_dict = new Dictionary<string, string>();
            
            int attempt_cnt = 0;
            double accumulator = 0;
            do
            {
                // Clear the dictionary and start the timer
                cipher_dict.Clear();
                Stopwatch sw = new Stopwatch();
                sw.Start();

                //Seed the dictionary with first hash to match
                string phrase_seed = BuildString2();
                string phrase_bytes_str = GetFirst24bits(phrase_seed);
                cipher_dict.Add(phrase_seed, phrase_bytes_str); 

                bool contains_first24;
                int counter = 0;
                do
                {
                    // Get a hash value and search for it in the dictionary
                    string input_str = BuildString();
                    string rand_str = GetFirst24bits(input_str);
                    contains_first24 = cipher_dict.ContainsValue(rand_str);

                    // If match is found, log the finding
                    if (contains_first24)
                    {
                        sw.Stop();
                        string elapsed = sw.Elapsed.ToString();
                        log.Add($"Two strings both hashed to {rand_str} after {counter} tries.      Time elapsed: {elapsed}");
                    } else
                    {
                        // Add it to the dictionary for the next round
                        cipher_dict.Add(input_str, rand_str);
                        counter++;
                        
                    }

                } while ((!contains_first24) && (counter < 35000000));

                if(sw.IsRunning)
                {
                    sw.Stop();
                }

                accumulator += counter;
                attempt_cnt++;
            } while (attempt_cnt < 100);

            log.Add($"\n\nAverage number of attempts to find the first match was: {accumulator / attempt_cnt}");
            PrintLog(log);
        }

        // Builds a random string 5 to 20 chars long using all possible keyboard chars
        static string BuildString()
        {
            Random rand_size = new Random();
            int rand_char_size = rand_size.Next(5, 20);
            char[] chars = new char[rand_char_size];

            for (int i = 0; i < chars.Length; i++)
            {
                Random rand = new Random();
                int rand_char = rand.Next(0, GetChars().Length);
                chars[i] = GetChars()[rand_char];
            }

            string out_str = new string(chars);
            return out_str;
        }

        // Accepts a List of strings and writes it to the console
        static void PrintLog(List<string> log)
        {
            for (int i = 0; i < log.Count; i++)
            {
                Console.WriteLine(log[i]);
            }
        }

        // Builds a random strin of random length between 5 and 10 chars long.
        static string BuildString2()
        {
            // Define new phrase length
            Random rand_size = new Random();
            int rand_char_size = rand_size.Next(5, 10);
            char[] chars = new char[rand_char_size];

            // Define chars to use
            string char_list = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] possible_chars = char_list.ToCharArray();

            // Build the phrase
            for (int i = 0; i < chars.Length; i++)
            {
                Random rand = new Random();
                int rand_char = rand.Next(0, possible_chars.Length);
                chars[i] = possible_chars[rand_char];
            }

            string out_str = new string(chars);
            return out_str;
        }

        // Returns all keyboard characters in a char array
        static char[] GetChars()
        {
            string char_list = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ`~!@#$%^&*()-_=+[]{}|;':,./<>? ";
            char[] possible_chars = char_list.ToCharArray();
            Array.Resize(ref possible_chars, possible_chars.Length + 2);
            possible_chars[possible_chars.Length - 2] = (char)0x22;
            possible_chars[possible_chars.Length - 1] = (char)0x5C;

            return possible_chars;
        }

        // Accepts a string and returns returns the first 24 bits (3 bytes) of the string's hash
        static string GetFirst24bits(string phrase)
        {
            byte[] hashValue;

            using (SHA256 mySHA256 = SHA256.Create())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(phrase);
                hashValue = mySHA256.ComputeHash(bytes);
            }
            byte[] first24 = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                first24[i] = hashValue[i];
            }

            return BytesToStr(first24);
        }

        // Create a hex string from a bytes array 
        static string BytesToStr(byte[] bytes_arr)
        {
            return BitConverter.ToString(bytes_arr).Replace("-", "");
        }
    }
}
