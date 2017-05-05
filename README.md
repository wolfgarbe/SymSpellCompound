# SymSpellCompound


__Compound aware automatic spelling correction__

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

__Performance:__

0.2 milliseconds / word  
5000 words / second  (Single Core on 2012 Macbook Pro)


[SymSpell](https://github.com/wolfgarbe/symspell) improved correction speed by six orders of magnitude, 
__SymSpellCompound__ goes beyond that and adresses the following challenges:

__Compound splitting & decompounding__

SymSpell assumed every input string as _single term_. SymSpellCompound supports _compound splitting / decompounding_ with three cases:
1. mistakenly _inserted space into a correct word_ led to two incorrect terms 
2. mistakenly _omitted space between two correct words_ led to one incorrect combined term
3. _multiple input terms_ with/without spelling errors

__Automatic spelling correction & context awareness__

* To digitize paper-based archives OCR is used, which introduces errors. Large archives make manual correction infeasible and require unsupervised, fully-automatic spelling correction. 
* In normal spelling correction, the user is presented with spelling correction suggestions. The user then has to make the right choice based on context.
* For automatic spelling correction the context has to be analyzed and interpreted by the algorithm itself to make an educated choice.

__Dictionary quality__

Dictionary quality is paramount for correction quality. 
The algorithm shouldn't correct to pseudo words which do not exists, because the dictionary contains spelling errors and word frequencies are not representative.
It also shouldn't be unable to provide corrections for some words, because the dictionary is incomplete.
SymSpell used big.txt to derive the dictionary solely for benchmarking reasons, but it did not provide the completeness & statistical properties required for professional spelling correction.

__Frequency dictionary & correction quality__

A frequency dictionary which accurately reflects the frequency of words is paramount for correction quality. 
quality of spelling correction is a statistical measurement. The algorithm should make the right spelling correction choice most of the time.
If 2 corrections have the same edit distance, it should use the more probable one. 
It need to know the probability also for rare terms, because most of the word belong to the long tail (zipfean distribution).  
	
__Bigram frequency dictionary__

If 2 corrections have the same edit distance, and about the same frequency, the algorithm still can decide by context. Some word pairs are much more likely then others (markov chains)
This kind of context can be derived from a bigram frequency dictionary.

__Applications__

Query correction, ocr post-processing, orthographic quality assessment, agent & chat bot conversation.
