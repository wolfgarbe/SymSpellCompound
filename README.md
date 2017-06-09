# SymSpellCompound


__Compound aware automatic spelling correction__

__SymSpellCompound__ supports __compound__ aware __automatic__ spelling correction of __multi-word input__ strings. <br>It is built on top of [SymSpell](https://github.com/wolfgarbe/symspell)'s __1 million times faster__ spelling correction algorithm.

__1. Compound splitting & decompounding__

SymSpell assumed every input string as _single term_. SymSpellCompound supports _compound splitting / decompounding_ with three cases:
1. mistakenly __inserted space within a correct word__ led to two incorrect terms 
2. mistakenly __omitted space between two correct words__ led to one incorrect combined term
3. __multiple input terms__ with/without spelling errors

Splitting errors, concatenation errors, substitution errors, transposition errors, deletion errors and insertion errors can by mixed within the same word.

__2. Automatic spelling correction__

* Large document collections make manual correction infeasible and require unsupervised, fully-automatic spelling correction. 
* In conventional spelling correction of a single token, the user is presented with spelling correction suggestions. <br>For automatic spelling correction of long multi-word text the the algorithm itself has to make an educated choice.

__Examples:__

```diff
- whereis th elove hehad dated forImuch of thepast who couqdn'tread in sixthgrade and ins pired him
+ where is the love he had dated for much of the past who couldn't read in sixth grade and inspired him  (9 edits)

- in te dhird qarter oflast jear he hadlearned ofca sekretplan y iran
+ in the third quarter of last year he had learned of a secret plan by iran  (10 edits)

- the bigjest playrs in te strogsommer film slatew ith plety of funn
+ the biggest players in the strong summer film slate with plenty of fun  (9 edits)

- Can yu readthis messa ge despite thehorible sppelingmsitakes
+ can you read this message despite the horrible spelling mistakes  (9 edits)
```

__Performance__

0.2 milliseconds / word  
5000 words / second  (single core on 2012 Macbook Pro)

__Applications__

* Query correction (10–15% of queries contain misspelled terms),
* Chatbots,
* OCR post-processing,
* Automated proofreading.

__Frequency dictionary__

The [word frequency list](https://github.com/wolfgarbe/SymSpellCompound/blob/master/wordfrequency_en.txt) was created by intersecting the two lists mentioned below. By reciprocally filtering only those words which appear in both lists are used. Additional filters were applied and the resulting list truncated to &#8776; 80,000 most frequent words.
* [Google Books Ngram data](http://storage.googleapis.com/books/ngrams/books/datasetsv2.html)   [(License)](https://creativecommons.org/licenses/by/3.0/) : Provides representative word frequencies
* [SCOWL - Spell Checker Oriented Word Lists](http://wordlist.aspell.net/)   [(License)](http://wordlist.aspell.net/scowl-readme/) : Ensures genuine English vocabulary    

__Blog Posts: Algorithm, Benchmarks, Applications__

[1000x Faster Spelling Correction algorithm](http://blog.faroo.com/2012/06/07/improved-edit-distance-based-spelling-correction/)<br>
[1000x Faster Spelling Correction: Source Code released](http://blog.faroo.com/2012/06/24/1000x-faster-spelling-correction-source-code-released/)<br>
[Fast approximate string matching with large edit distances in Big Data](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/)<br> 
[Very fast Data cleaning of product names, company names & street names](http://blog.faroo.com/2015/09/29/how-to-correct-company-names-street-names-product-names/)<br>
[Sub-millisecond compound aware automatic spelling correction](https://medium.com/@wolfgarbe/symspellcompound-10ec8f467c9b)
<br><br>
**C#** (original source code)<br>
https://github.com/wolfgarbe/SymSpellCompound

**.NET** (NuGet package)<br>
https://www.nuget.org/packages/SymSpellCompound

```
Copyright (C) 2017 Wolf Garbe
Version: 1.0
Author: Wolf Garbe <wolf.garbe@faroo.com>
Maintainer: Wolf Garbe <wolf.garbe@faroo.com>
License:
This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License, 
version 3.0 (LGPL-3.0) as published by the Free Software Foundation.
http://www.opensource.org/licenses/LGPL-3.0
```
Usage: multiple words + Enter:  Display spelling suggestions
       Enter without input:     Terminate the program

<br><br>
