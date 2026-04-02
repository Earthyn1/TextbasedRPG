EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)

-> Intro


=== Intro ===
{ hasFlag("stableboy.met"): -> IntroRepeat }

~ setFlag("stableboy.met")

The lad leans on his broom. "You're late."

* ["Had things to do."]
    "Oh yeah? Tell him that yourself?"
    -> TalkLoop

* ["Any news from town?"]
    -> Rumors

* ["Back to work."]
    -> Goodbye


=== IntroRepeat ===
Tom gives you a nod. Lester's tail thumps once.
"You forget something?"

* ["Any news from town?"]
    -> Rumors

* ["(Look at Lester)"]
    -> DogLester

* ["Back to work."]
    -> Goodbye


=== TalkLoop ===
+ ["Any news from town?"]
    -> Rumors

+ ["(Look at Lester)"]
    -> DogLester

* ["Back to work."]
    -> Goodbye


=== Rumors ===
{ hasFlag("stableboy.askedRumors"): -> RumorsRepeat }

~ setFlag("stableboy.askedRumors")

"Guild's been packed. Folks coming out the dungeon half-dead again."
"Priests charging double if you're not copper-tier."

+ ["Think the dungeon's getting worse?"]
    -> Changed

+ ["Priests are the same everywhere."]
    -> PopsSays

+ ["Right."]
    "Tom starts to idly pet Lester."
    -> TalkLoop


=== RumorsRepeat ===
He side-eyes you. "Same as I told you. Dungeon's bad. Priests worse."

+ ["Yeah, yeah."]
    "Tom starts to idly pet Lester."
    -> TalkLoop

+ ["Alright."]
    -> Goodbye


=== Changed ===
He snorts. "If I knew that, I wouldn't be shoveling this."

-> TalkLoop


=== PopsSays ===
"Aye. Pops says they'd tax breathing if they could."

-> TalkLoop


=== DogLester ===
"Lester's been eyeing you all morning. Thinks you owe him something."

+ ["Good dog."]
    -> BiteYou

+ ["Not today."]
    "A low growl rumbles from Lester."
    -> TalkLoop


=== BiteYou ===
"If he bites you, I'm not helping."

-> TalkLoop


=== Goodbye ===
"Try not to make more work for me."
-> END