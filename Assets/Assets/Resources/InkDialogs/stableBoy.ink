EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)

-> Intro


=== Intro ===
{  hasFlag("Firstime"):
The lad glances up from a broom. “Still here are you?”

* [Any news from town?]
    -> Rumors

* [\(Glance at the dog\)]
    -> DogLester
    
    * [I’ll get out of your way.]
    -> Goodbye
    
    - else:
        ~ setFlag("Firstime")
        
        The lad glances up from a broom, hay stuck to his sleeves. “You ain’t from around here, are you?”

        * [That obvious huh? Came in two days past.]
            -> Warm
        
        * [Nope, any news from town?]
            -> Rumors
        
        * [\(Glance at the dog\)]
            -> DogLester
    
}

=== Warm ===
He brightens a little. “Name’s Tom. I keep the stalls clean an the horses calmer”

* [Any news from town?]
    -> Rumors

* [I’ll get out of your way.]
    -> Goodbye


=== Rumors ===
{  hasFlag("askedRumors"):
     
      He squints at you. “Already told you what I know. Dungeon’s been rough. Priests greedy. Nothin’ new since last breath.”
      
      * [Right sorry.]
    -> Goodbye
    
- else:
~ setFlag("askedRumors")
   “Guild’s been busier. Folks comin out the dungeon more beaten up than usual. Priests chargin double if you’re not copper-tier too.”
   
    * [What changed in the dungeon you think?]
    -> Changed

    * [Damned priests. Same in every town, eh?]
    -> PopsSays

    * [I’ll get out of your way.]
    -> Goodbye
}




=== DogLester ===
A grin. “Lester’s friendly enough. Toss him a bone and he’ll guard your boots like treasure.”

* [Thats a good boy.]
    -> BiteYou

* [I’ll get out of your way.]
    -> Goodbye


=== Goodbye ===
“I’ve chores. Don’t step in the bucket by the door... ain’t water.”
-> END


=== PopsSays ===
“Aye, Pops says the same — swears priests never heal what they can’t tax.”

* [\(Glance at the dog\)]
    -> DogLester

* [I’ll get out of your way.]
    -> Goodbye


=== Changed ===
“Thinkin I would be sweeping floors here if I knew that answer?”

* [Right. I'll get out of your way.]
    -> Goodbye


=== BiteYou ===
"If he bites you… that’s on you!”

* [Any news from town?]
    -> Rumors

* [I’ll get out of your way.]
    -> Goodbye