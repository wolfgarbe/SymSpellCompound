// SymSpellCompund: Compound aware automatic spelling correction
//
// SymSpellCompound supports compound aware automatic spelling correction of multi-word input strings. 
// It is built on top of SymSpell's 1 million times faster Symmetric Delete spelling correction algorithm.
//
// SymSpellCompound supports compound splitting / decompounding with three cases:
// 1. mistakenly inserted space into a correct word led to two incorrect terms 
// 2. mistakenly omitted space between two correct words led to one incorrect combined term
// 3. multiple independent input terms with/without spelling errors
//
// The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup 
// for a given Damerau-Levenshtein distance. It is six orders of magnitude faster and language independent.
// Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
// Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
// Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!
//
//
// Copyright (C) 2017 Wolf Garbe
// Version: 1.0
// Author: Wolf Garbe <wolf.garbe@faroo.com>
// Maintainer: Wolf Garbe <wolf.garbe@faroo.com>
// URL: https://github.com/wolfgarbe/SymSpellCompound
// Description: https://github.com/wolfgarbe/SymSpellCompound
//
// License:
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License, 
// version 3.0 (LGPL-3.0) as published by the Free Software Foundation.
// http://www.opensource.org/licenses/LGPL-3.0
//
// Usage: multiple words + Enter:  Display spelling suggestions
//        Enter without input:  Terminate the program

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;

static class SymSpell
{
    private static bool enableCompoundCheck = true;
    //false: assumes input string as single term, no compound splitting / decompounding
    //true:  supports compound splitting / decompounding with three cases:
    //1. mistakenly inserted space into a correct word led to two incorrect terms 
    //2. mistakenly omitted space between two correct words led to one incorrect combined term
    //3. multiple independent input terms with/without spelling errors

    private static int editDistanceMax=2;

    private static int verbose = 0; //ALLWAYS use verbose = 0 if enableCompoundCheck = true!
    //0: top suggestion
    //1: all suggestions of smallest edit distance   
    //2: all suggestions <= editDistanceMax (slower, no early termination)

    private class dictionaryItem
    {
        public List<Int32> suggestions = new List<Int32>(2);
        public Int64 count = 0;
    }
    
    private class suggestItem
    {
        public string term = "";
        public int distance = 0;
        public Int64 count = 0;

        public override bool Equals(object obj)
        {
            return Equals(term, ((suggestItem)obj).term);
        }
     
        public override int GetHashCode()
        {
            return term.GetHashCode();
        }

        //http://www.cshandler.com/2012/07/shallow-copy-and-deep-copy-using.html#.WQMxhVWGNaQ
        public suggestItem ShallowCopy()
        {
            return (suggestItem)MemberwiseClone();
        }
    }

    //Dictionary that contains both the original words and the deletes derived from them. A term might be both word and delete from another word at the same time.
    //For space reduction a item might be either of type dictionaryItem or Int. 
    //A dictionaryItem is used for word, word/delete, and delete with multiple suggestions. Int is used for deletes with a single suggestion (the majority of entries).
    //A Dictionary with fixed value type (int) requires less memory than a Dictionary with variable value type (object)
    //To support two types with a Dictionary with fixed type (int), positive number point to one list of type 1 (string), and negative numbers point to a secondary list of type 2 (dictionaryEntry)
    private static Dictionary<string, Int32> dictionary = new Dictionary<string, Int32>(); //initialisierung

    //List of unique words. By using the suggestions (Int) as index for this list they are translated into the original string.
    private static List<string> wordlist = new List<string>();
    private static List<dictionaryItem> itemlist = new List<dictionaryItem>(); 

    //create a non-unique wordlist from sample text
    //language independent (e.g. works with Chinese characters)
    private static IEnumerable<string> parseWords(string text)
    {
        // \w Alphanumeric characters (including non-latin characters, umlaut characters and digits) plus "_" 
        // \d Digits
        // Provides identical results to Norvigs regex "[a-z]+" for latin characters, while additionally providing compatibility with non-latin characters

        //for benchmarking only, to provide the exact same number of dictionary items to Norvig's algorithm, but splits words at apostrophes 
        //return Regex.Matches(text.ToLower(), @"[\w-[\d_]]+").Cast<Match>().Select(m => m.Value);
        return Regex.Matches(text.ToLower(), @"['’\w-[_]]+").Cast<Match>().Select(m => m.Value);
    }

    public static int maxlength = 0;//maximum dictionary term length

    //for every word there all deletes with an edit distance of 1..editDistanceMax created and added to the dictionary
    //every delete entry has a suggestions list, which points to the original term(s) it was created from
    //The dictionary may be dynamically updated (word frequency and new words) at any time by calling createDictionaryEntry
    private static bool CreateDictionaryEntry(string key, string language, Int64 count) //count=0 : value.count++/ count>0 : value.count=count
    {
        //a treshold might be specifid, when a term occurs so frequently in the corpus that it is considered a valid word for spelling correction
        int countTreshold = 1;
        Int64 countPrevious = 0;
        bool result = false;
        dictionaryItem value = null;
        Int32 valueo;
        if (dictionary.TryGetValue(language+key, out valueo))
        {           
            //new word, but identical single delete existed before
            //+ = single delete = index auf worlist 
            //- = !single delete (word / word + delete(s) / deletes) = index to dictionaryItem list
            if (valueo>=0) 
            {
                Int32 tmp = valueo; 
                value = new dictionaryItem();
                value.suggestions.Add(tmp); value.suggestions.TrimExcess();
                itemlist.Add(value);
                dictionary[language + key] = -itemlist.Count;
            }
            //existing word (word appears several times)
            else
            {
                value = itemlist[-valueo - 1];
            }

            countPrevious = value.count;
            //summarizes multiple frequency entries of a word
            if (count > 0) value.count +=count;
            //prevent overflow (repetitions of a word)
            else if (value.count < Int64.MaxValue) value.count++;
        }
        else 
        {
            //new word
            value = new dictionaryItem();
            if (count > 0) value.count = count; else value.count++;
            itemlist.Add(value); 
            dictionary[language + key] = -itemlist.Count;

            if (key.Length > maxlength) maxlength = key.Length;
        }


        //edits/suggestions are created only once, no matter how often word occurs
        //edits/suggestions are created only as soon as the word occurs in the corpus, 
        //even if the same term existed before in the dictionary as an edit from another word
        if ((value.count >=countTreshold) && (countPrevious< countTreshold))
        {
            //word2index
            wordlist.Add(key);
            Int32 keyint = (Int32)(wordlist.Count - 1);

            result = true;

            //create deletes
            foreach (string delete in Edits(key, 0, new HashSet<string>()))
            {
                Int32 value2;
                dictionaryItem di;
                if (dictionary.TryGetValue(language+delete, out value2))
                {
                    //already exists:
                    //1. word1==deletes(word2) 
                    //2. deletes(word1)==deletes(word2) 
                    //int or dictionaryItem? single delete existed before!
                    if (value2 >= 0)
                    {
                        //transformes int to dictionaryItem
                        di = new dictionaryItem(); di.suggestions.Add(value2);  di.suggestions.TrimExcess(); itemlist.Add(di); dictionary[language + delete] = -itemlist.Count;
                        if (!di.suggestions.Contains(keyint)) AddLowestDistance(di, key, keyint, delete);
                    }
                    else
                    {
                        di = itemlist[-value2 - 1];
                        if (!di.suggestions.Contains(keyint)) AddLowestDistance(di, key, keyint, delete);
                    }
                }
                else
                {
                    dictionary.Add(language + delete, keyint);         
                }

            }
        }
        return result;
    }

    public static Dictionary<string,Int64> bigrams = new Dictionary<string, Int64>();

    //load a frequency dictionary
    private static void LoadDictionary(string corpus, string language, int termIndex, int countIndex)
    {
        //frequency dictionary
        if (!File.Exists(corpus))
        {
            Console.Error.WriteLine("File not found: " + corpus);
            return;
        }

        Console.Write("Creating dictionary ...");
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        long wordCount = 0;

        using (StreamReader sr = new StreamReader(corpus))
        {
            String line;

            //process a single line at a time only for memory efficiency
            while ((line = sr.ReadLine()) != null)
            {              
                string[] lineParts = line.Split(null);
                if (lineParts.Length>=2)
                {           
                    string key = lineParts[termIndex];
                    Int64 count;
                    if (Int64.TryParse(lineParts[countIndex], out count))
                    {

                        if (CreateDictionaryEntry(key, language, Math.Min(Int64.MaxValue, count))) wordCount++;
                    }
                }
            }
        }

        wordlist.TrimExcess();
        stopWatch.Stop();
        Console.WriteLine("\rDictionary: " + wordCount.ToString("N0") + " words, " + dictionary.Count.ToString("N0") + " entries, edit distance=" + editDistanceMax.ToString() + " in " + stopWatch.ElapsedMilliseconds.ToString() + "ms " + (Process.GetCurrentProcess().PrivateMemorySize64 / 1000000).ToString("N0") + " MB");
    }

    //create a frequency dictionary from a corpus
    private static void CreateDictionary(string corpus, string language)
    {
        if (!File.Exists(corpus))
        {
            Console.Error.WriteLine("File not found: " + corpus);
            return;
        }

        Console.Write("Creating dictionary ...");
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        long wordCount = 0;

        using (StreamReader sr = new StreamReader(corpus))
        {
            String line;
            //process a single line at a time only for memory efficiency
            while ((line = sr.ReadLine()) != null)
            {
                foreach (string key in parseWords(line))
                {
                   if ((key.Length == 1) && (key != "a") && (key != "i")) continue; 
                   if (CreateDictionaryEntry(key, language,0)) wordCount++;
                }
            }
        }

        wordlist.TrimExcess();
        stopWatch.Stop();
        Console.WriteLine("\rDictionary: " + wordCount.ToString("N0") + " words, " + dictionary.Count.ToString("N0") + " entries, edit distance=" + editDistanceMax.ToString() + " in " + stopWatch.ElapsedMilliseconds.ToString()+"ms "+ (Process.GetCurrentProcess().PrivateMemorySize64/1000000).ToString("N0")+ " MB");
    }

    //save some time and space
    private static void AddLowestDistance(dictionaryItem item, string suggestion, Int32 suggestionint, string delete)
    {
        //remove all existing suggestions of higher distance, if verbose<2
        //index2word
        if ((verbose < 2) && (item.suggestions.Count > 0) && (wordlist[item.suggestions[0]].Length-delete.Length > suggestion.Length - delete.Length)) item.suggestions.Clear();
        //do not add suggestion of higher distance than existing, if verbose<2
        if ((verbose == 2) || (item.suggestions.Count == 0) || (wordlist[item.suggestions[0]].Length - delete.Length >= suggestion.Length - delete.Length)) { item.suggestions.Add(suggestionint); item.suggestions.TrimExcess(); }
    }

    //inexpensive and language independent: only deletes, no transposes + replaces + inserts
    //replaces and inserts are expensive and language dependent (Chinese has 70,000 Unicode Han characters)
    private static HashSet<string> Edits(string word, int editDistance, HashSet<string> deletes)
    {
        editDistance++;
        if (word.Length > 1)
        {
            for (int i = 0; i < word.Length; i++)
            {
                string delete = word.Remove(i, 1);
                if (deletes.Add(delete))
                {
                    //recursion, if maximum edit distance not yet reached
                    if (editDistance < editDistanceMax) Edits(delete, editDistance, deletes);
                }
            }
        }
        return deletes;
    }

    private static List<suggestItem> Lookup(string input, string language, int editDistanceMax)
    {
        //save some time
        if (input.Length - editDistanceMax > maxlength) return new List<suggestItem>();

        List<string> candidates = new List<string>();
        HashSet<string> hashset1 = new HashSet<string>();
 
        List<suggestItem> suggestions = new List<suggestItem>();
        HashSet<string> hashset2 = new HashSet<string>();

        Int32 valueo;

        //add original term
        candidates.Add(input);

        while (candidates.Count>0)
        {
            string candidate = candidates[0];
            candidates.RemoveAt(0);

            //save some time
            //early termination
            //suggestion distance=candidate.distance... candidate.distance+editDistanceMax                
            //if canddate distance is already higher than suggestion distance, than there are no better suggestions to be expected
            if ((verbose < 2) && (suggestions.Count > 0) && (input.Length-candidate.Length > suggestions[0].distance)) goto sort;

            //read candidate entry from dictionary
            if (dictionary.TryGetValue(language + candidate, out valueo))
            {
                dictionaryItem value = new dictionaryItem();
                if (valueo >= 0) value.suggestions.Add((Int32)valueo); else value = itemlist[-valueo - 1];

                //if count>0 then candidate entry is correct dictionary term, not only delete item
                //if ((value.count > 0) && hashset2.Add(candidate))
                //New: supress short, infrequent terms as suggestion, because that are probably spelling errors that made it to the dictionary
                if (((value.count > 100)||((candidate.Length > 2)&&(value.count > 0)) ) && hashset2.Add(candidate))
                {
                    int distance = input.Length - candidate.Length;
                    //save some time
                    //do not process higher distances than those already found, if verbose<2      
                    if ((verbose == 2) || (suggestions.Count == 0) || (distance <= suggestions[0].distance))
                    {
                        //Fix: previously not allways all suggestions were returned for verbose<2 : e.g. elove did not return love (edit distance=0)
                        //suggestions.Clear() was not executed in this branch, if a suggestion with lower edit distance was added here (for verbose<2). 
                        //Then possibly suggestions with higher edit distance remained on top, the suggestion with lower edit distance were added to the end. 
                        //All of them where deleted later once a suggestion with a lower distance than the first item in the list was later added in the other branch. 
                        //Therefore returned suggestions were not always complete for verbose<2.
                        //remove all existing suggestions of higher distance, if verbose<2
                        if ((verbose < 2) && (suggestions.Count > 0) && (suggestions[0].distance > distance)) suggestions.Clear();

                        //add correct dictionary term term to suggestion list
                        suggestItem si = new suggestItem();
                        si.term = candidate;
                        si.count = value.count;
                        si.distance = distance;
                        suggestions.Add(si);
                        //early termination
                        if ((verbose < 2) && (input.Length - candidate.Length == 0)) goto sort;
                    }
                }

                //iterate through suggestions (to other correct dictionary items) of delete item and add them to suggestion list
                Int32 value2;
                foreach (int suggestionint in value.suggestions)
                {
                    //save some time 
                    //skipping double items early: different deletes of the input term can lead to the same suggestion
                    //index2word
                    string suggestion = wordlist[suggestionint];
                    if (hashset2.Add(suggestion))
                    {
                        //True Damerau-Levenshtein Edit Distance: adjust distance, if both distances>0
                        //We allow simultaneous edits (deletes) of editDistanceMax on on both the dictionary and the input term. 
                        //For replaces and adjacent transposes the resulting edit distance stays <= editDistanceMax.
                        //For inserts and deletes the resulting edit distance might exceed editDistanceMax.
                        //To prevent suggestions of a higher edit distance, we need to calculate the resulting edit distance, if there are simultaneous edits on both sides.
                        //Example: (bank==bnak and bank==bink, but bank!=kanb and bank!=xban and bank!=baxn for editDistanceMaxe=1)
                        //Two deletes on each side of a pair makes them all equal, but the first two pairs have edit distance=1, the others edit distance=2.
                        int distance = 0;
                        if (suggestion != input)
                        {
                            if (suggestion.Length == candidate.Length) distance = input.Length - candidate.Length;
                            else if (input.Length == candidate.Length) distance = suggestion.Length - candidate.Length;
                            else
                            {
                                //common prefixes and suffixes are ignored, because this speeds up the Damerau-levenshtein-Distance calculation without changing it.
                                int ii = 0;
                                int jj = 0;
                                while ((ii < suggestion.Length) && (ii < input.Length) && (suggestion[ii] == input[ii])) ii++;
                                while ((jj < suggestion.Length - ii) && (jj < input.Length - ii) && (suggestion[suggestion.Length - jj - 1] == input[input.Length - jj - 1])) jj++;
                                if ((ii > 0) || (jj > 0)) { distance = DamerauLevenshteinDistance(suggestion.Substring(ii, suggestion.Length - ii - jj), input.Substring(ii, input.Length - ii - jj)); } else distance = DamerauLevenshteinDistance(suggestion, input);

                            }
                        }

                        //save some time
                        //do not process higher distances than those already found, if verbose<2
                        if ((verbose < 2) && (suggestions.Count > 0) && (distance > suggestions[0].distance)) continue; 
                        if (distance <= editDistanceMax)
                        {
                            if (dictionary.TryGetValue(language + suggestion, out value2))
                            {
                                suggestItem si = new suggestItem();
                                si.term = suggestion;
                                si.count = itemlist[-value2 - 1].count;
                                si.distance = distance;

                                //New: supress short, infrequent terms as suggestion, because that are probably spelling errors that made it to the dictionary
                                if ((si.count > 100) || (suggestion.Length > 2)) 
                                {
                                    //remove all existing suggestions of higher distance, if verbose<2
                                    if ((verbose < 2) && (suggestions.Count > 0) && (suggestions[0].distance > distance)) suggestions.Clear();
                                    suggestions.Add(si);
                                }
                            }
                        }
                    }
                }//end foreach
            }//end if         
            
            //add edits 
            //derive edits (deletes) from candidate (input) and add them to candidates list
            //this is a recursive process until the maximum edit distance has been reached
            if (input.Length - candidate.Length < editDistanceMax)
            {
                //save some time
                //do not create edits with edit distance smaller than suggestions already found
                if ((verbose < 2) && (suggestions.Count > 0) && (input.Length - candidate.Length >= suggestions[0].distance)) continue;

                for (int i = 0; i < candidate.Length; i++)
                {
                    string delete = candidate.Remove(i, 1);
                    if (hashset1.Add(delete)) candidates.Add(delete);
                }
            }
        }//end while

        //sort by ascending edit distance, then by descending word frequency
        sort: if (verbose < 2) suggestions.Sort((x, y) => -x.count.CompareTo(y.count)); else suggestions.Sort((x, y) => 2*x.distance.CompareTo(y.distance) - x.count.CompareTo(y.count));
        if ((verbose == 0)&&(suggestions.Count>1)) return suggestions.GetRange(0, 1); else return suggestions;
    }

    private static void Correct(string input, string language)
    {
        List<suggestItem> suggestions = null;
        
        //check in dictionary for existence and frequency; sort by ascending edit distance, then by descending word frequency     
        if (enableCompoundCheck) suggestions = LookupCompound(input, language, editDistanceMax); else suggestions = Lookup(input, language, editDistanceMax);

        //display term and frequency
        foreach (var suggestion in suggestions)
        {
            Console.WriteLine( suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString());
        }
        if (verbose !=0) Console.WriteLine(suggestions.Count.ToString() + " suggestions");
    }


    private static List<suggestItem> LookupCompound(string input, string language, int editDistanceMax)
    {
        //parse input string into single terms
        string[] termList1 = parseWords(input).ToArray();

        List<suggestItem> suggestionsPreviousTerm;                  //suggestions for a single term
        List<suggestItem> suggestions= new List<suggestItem>();     //suggestions for a single term
        List<suggestItem> suggestionParts = new List<suggestItem>(); //1 line with separate parts

        //translate every term to its best suggestion, otherwise it remains unchanged
        bool lastCombi = false;
        for (int i = 0; i < termList1.Length; i++)
        {
            suggestionsPreviousTerm = new List<suggestItem>(suggestions.Count); for (int k = 0; k < suggestions.Count; k++) suggestionsPreviousTerm.Add(suggestions[k].ShallowCopy());
            suggestions = Lookup(termList1[i], language, editDistanceMax);


            //combi check, always before split
            if ((i>0)&&!lastCombi)
            { 
                List<suggestItem> suggestionsCombi = Lookup(termList1[i - 1] + termList1[i], language, editDistanceMax);
                
                if (suggestionsCombi.Count>0)
                {
                    suggestItem best1 = suggestionParts[suggestionParts.Count - 1];
                    suggestItem best2 = new suggestItem();
                    if (suggestions.Count > 0)
                    {
                        best2 = suggestions[0];
                        
                    } else
                    {
                        best2.term = termList1[i];
                        best2.distance = editDistanceMax + 1;
                        best2.count = 0;
                    }                   
                    if (suggestionsCombi[0].distance + 1 < DamerauLevenshteinDistance(termList1[i - 1] + " " + termList1[i], best1.term + " " + best2.term))
                    { 
                        suggestionsCombi[0].distance++; 
                        suggestionParts[suggestionParts.Count - 1] = suggestionsCombi[0];
                        lastCombi = true;
                        goto nextTerm;
                    }
                }
            }
            lastCombi = false;

            //alway split terms without suggestion / never split terms with suggestion ed=0 / never split single char terms
            if ((suggestions.Count > 0)&&((suggestions[0].distance==0)||(termList1[i].Length == 1)))
            {
                //choose best suggestion
                suggestionParts.Add(suggestions[0]);
            }
            else
            {
                //if no perfect suggestion, split word into pairs
                List<suggestItem> suggestionsSplit = new List<suggestItem>();

                //add original term
                if (suggestions.Count > 0) suggestionsSplit.Add(suggestions[0]);

                if (termList1[i].Length>1)
                {
                    
                    for (int j=1;j< termList1[i].Length;j++)
                    {
                        string part1 = termList1[i].Substring(0,j);
                        string part2 = termList1[i].Substring(j);
                        suggestItem suggestionSplit = new suggestItem();                        
                        List<suggestItem> suggestions1 = Lookup(part1, language, editDistanceMax);
                        if (suggestions1.Count>0)
                        {
                            if ((suggestions.Count > 0) && (suggestions[0].term == suggestions1[0].term)) break;//if split correction1 == einzelwort correction
                            List<suggestItem> suggestions2 = Lookup(part2, language, editDistanceMax);
                            if (suggestions2.Count > 0)
                            {
                                if ((suggestions.Count > 0) && (suggestions[0].term == suggestions2[0].term)) break;//if split correction1 == einzelwort correction
                                //select best suggestion for split pair
                                suggestionSplit.term = suggestions1[0].term + " " + suggestions2[0].term;
                                suggestionSplit.distance=DamerauLevenshteinDistance(termList1[i], suggestions1[0].term + " " + suggestions2[0].term);
                                suggestionSplit.count = Math.Min(suggestions1[0].count, suggestions2[0].count);
                                suggestionsSplit.Add(suggestionSplit);

                                //early termination of split
                                if (suggestionSplit.distance == 1) break;
                            }
                        }                   
                    }

                    if (suggestionsSplit.Count>0)
                    {
                        //select best suggestion for split pair
                        suggestionsSplit.Sort((x, y) => 2 * x.distance.CompareTo(y.distance) - x.count.CompareTo(y.count));
                        suggestionParts.Add(suggestionsSplit[0]);
                    }
                    else
                    {
                        suggestItem si = new suggestItem();
                        si.term = termList1[i];
                        si.count = 0;
                        si.distance = editDistanceMax + 1;
                        suggestionParts.Add(si);
                    }
                }
                else
                {
                    suggestItem si = new suggestItem();
                    si.term = termList1[i];
                    si.count = 0;
                    si.distance = editDistanceMax + 1;
                    suggestionParts.Add(si);
                }
            }
        nextTerm:;
        }

        suggestItem suggestion = new suggestItem();                  
        suggestion.count = Int64.MaxValue;
        string s = ""; foreach (suggestItem si in suggestionParts) { s += si.term + " "; suggestion.count = Math.Min(suggestion.count,si.count); }//Console.WriteLine(s);
        suggestion.term = s.TrimEnd();
        suggestion.distance = DamerauLevenshteinDistance(suggestion.term, input);

        List<suggestItem> suggestionsLine = new List<suggestItem>();       
        suggestionsLine.Add(suggestion);
        return suggestionsLine;
    }



    private static void ReadFromStdIn()
    {
        string input;
        while (!string.IsNullOrEmpty(input = (Console.ReadLine() ?? "").Trim()))
        {
            Correct(input,"");
        }
    }

    public static void Main(string[] args)
    {
        //Create the dictionary from a sample corpus
        //e.g. http://norvig.com/big.txt , or any other large text corpus
        //The dictionary may contain vocabulary from different languages. 
        //If you use mixed vocabulary use the language parameter in Correct() and CreateDictionary() accordingly.
        //You may use CreateDictionaryEntry() to update a (self learning) dictionary incrementally
        //To extend spelling correction beyond single words to phrases (e.g. correcting "unitedkingom" to "united kingdom") simply add those phrases with CreateDictionaryEntry().
        //CreateDictionary("big.txt","");

        //Manually curating/cleaning up the dictionary or using a professional frequency dictionary will increase the precision of the suggestions.
        //Load a frequency dictionary
        LoadDictionary("wordfrequency_en.txt", "", 0, 1); 

        ReadFromStdIn();
    }

    // Damerau–Levenshtein distance algorithm and code 
    // from http://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance (as retrieved in June 2012)
    public static Int32 DamerauLevenshteinDistance(String source, String target)
    {
        Int32 m = source.Length;
        Int32 n = target.Length;
        Int32[,] H = new Int32[m + 2, n + 2];

        Int32 INF = m + n;
        H[0, 0] = INF;
        for (Int32 i = 0; i <= m; i++) { H[i + 1, 1] = i; H[i + 1, 0] = INF; }
        for (Int32 j = 0; j <= n; j++) { H[1, j + 1] = j; H[0, j + 1] = INF; }

        SortedDictionary<Char, Int32> sd = new SortedDictionary<Char, Int32>();
        foreach (Char Letter in (source + target))
        {
            if (!sd.ContainsKey(Letter))
                sd.Add(Letter, 0);
        }

        for (Int32 i = 1; i <= m; i++)
        {
            Int32 DB = 0;
            for (Int32 j = 1; j <= n; j++)
            {
                Int32 i1 = sd[target[j - 1]];
                Int32 j1 = DB;

                if (source[i - 1] == target[j - 1])
                {
                    H[i + 1, j + 1] = H[i, j];
                    DB = j;
                }
                else
                {
                    H[i + 1, j + 1] = Math.Min(H[i, j], Math.Min(H[i + 1, j], H[i, j + 1])) + 1;
                }

                H[i + 1, j + 1] = Math.Min(H[i + 1, j + 1], H[i1, j1] + (i - i1 - 1) + 1 + (j - j1 - 1));
            }

            sd[source[i - 1]] = i;
        }
        return H[m + 1, n + 1];
    }

}
