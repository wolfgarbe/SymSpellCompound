SymSpellCompound<br>
[![NuGet version](https://badge.fury.io/nu/symspellcompound.svg)](https://badge.fury.io/nu/symspellcompound)
========


### SymSpellCompound has been integrated into [SymSpell](https://github.com/wolfgarbe/SymSpell). Please visit the [SymSpell](https://github.com/wolfgarbe/SymSpell) repository!  

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
