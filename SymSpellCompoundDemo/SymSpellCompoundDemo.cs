using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

// uses SymSpellCompound.cs 
// *alternatively* use SymSpellCompound as NuGet package from https://www.nuget.org/packages/symspellcompound

// Usage: single word + Enter:  Display spelling suggestions
//        Enter without input:  Terminate the program

namespace SymSpellCompoundDemo
{
    class Program
    {	
        private static void Correct(string input, string language)
        {
            List<SymSpellCompound.suggestItem> suggestions = null;

            //check if input term or similar terms within edit-distance are in dictionary, return results sorted by ascending edit distance, then by descending word frequency     
			if (SymSpellCompound.enableCompoundCheck) suggestions = SymSpellCompound.LookupCompound(input, language, SymSpellCompound.editDistanceMax); else suggestions = SymSpellCompound.Lookup(input, language, SymSpellCompound.editDistanceMax);

            //display term and frequency
            foreach (var suggestion in suggestions)
            {
                Console.WriteLine( suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
            }
            if (SymSpellCompound.verbose != 0) Console.WriteLine(suggestions.Count.ToString() + " suggestions");
        }

        //Load a frequency dictionary or create a frequency dictionary from a text corpus
        public static void Main(string[] args)
        {
            //set global parameters
			SymSpellCompound.enableCompoundCheck=true;
            SymSpellCompound.verbose = 0;
            SymSpellCompound.editDistanceMax = 2;

            Console.Write("Creating dictionary ...");//neuneu
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //Load a frequency dictionary
            //wordfrequency_en.txt  ensures high correction quality by combining two data sources: 
            //Google Books Ngram data  provides representative word frequencies (but contains many entries with spelling errors)  
            //SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies)
            //string path = "../../wordfrequency_en.txt";           		 //path when using SymSpellCompound nuget package (wordfrequency_en.txt is included in nuget package)
            string path = "../../../SymSpellCompound/wordfrequency_en.txt";  //path when using SymSpellCompound.cs
            if (!SymSpellCompound.LoadDictionary(path, "", 0, 1)) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path)); 

            //Alternatively Create the dictionary from a text corpus (e.g. http://norvig.com/big.txt ) 
            //Make sure the corpus does not contain spelling errors, invalid terms and the word frequency is representative to increase the precision of the spelling correction.
            //The dictionary may contain vocabulary from different languages. 
            //If you use mixed vocabulary use the language parameter in Correct() and CreateDictionary() accordingly.
            //You may use SymSpellCompound.CreateDictionaryEntry() to update a (self learning) dictionary incrementally
            //To extend spelling correction beyond single words to phrases (e.g. correcting "unitedkingom" to "united kingdom") simply add those phrases with CreateDictionaryEntry().
            //string path = "big.txt"
            //if (!SymSpellCompound.CreateDictionary(path,"")) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path));

            Console.WriteLine("\rDictionary: " + SymSpellCompound.wordlist.Count.ToString("N0") + " words, " + SymSpellCompound.dictionary.Count.ToString("N0") + " entries, edit distance=" + SymSpellCompound.editDistanceMax.ToString() + " in " + stopWatch.ElapsedMilliseconds.ToString() + "ms "/*+ (Process.GetCurrentProcess().PrivateMemorySize64/1000000).ToString("N0")+ " MB"*/);//neuneu

            string input;
            while (!string.IsNullOrEmpty(input = (Console.ReadLine() ?? "").Trim()))
            {
                Correct(input, "");
            }
        }
    }
}
