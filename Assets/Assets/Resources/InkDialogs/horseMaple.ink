EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(minigameId)
EXTERNAL reportAction(actionId)
EXTERNAL questActive(questId)
EXTERNAL questEverCompleted(questId)

-> Intro

=== Intro ===
{ hasFlag("metMaple"):
    Maple turns her head toward you, ears forward, breathing slow and steady.
- else:
    ~ setFlag("metMaple")
    A dappled mare with a white blaze down her nose. She watches you approach with calm, dark eyes.
}

* [Hold out your hand]
    -> Sniff
* { hasItem("brush", 1) && questActive("GroomMaple") } [Brush her down.]
    -> StartGroom
* [Give her a pat.]
    -> Pat
* { hasItem("apple", 1) } [Offer an apple.]
    -> OfferFood
* [Leave her be.]
    -> Goodbye


=== Sniff ===
Maple lowers her muzzle to your palm, huffing warm breath across your fingers. She seems satisfied.
* [Give her a pat.]
    -> Pat
* [Leave her be.]
    -> Goodbye


=== Pat ===
She leans into your hand just slightly — not much, but enough.
* [Good girl.]
    -> Goodbye


=== OfferFood ===
~ giveItem("apple", -1)
You produce an apple. Maple takes it without hesitation, crunching contentedly. Her tail swishes once.
* [There you go.]
    -> Goodbye


=== StartGroom ===
Maple stands still as you raise the brush, one ear flicking toward you.
+ [Get to work.]
    You work the brush in slow strokes across her neck, watching her ears. She needs to stay calm — follow her rhythm.
    ~ startMinigame("groomMaple, 1, TimingBar")
-> END


=== MinigameFound ===
Long smooth strokes. Maple's coat starts to shine. She exhales slowly — content.
~ giveItem("brush", -1)
~ reportAction("Action_Stable_GroomMaple")
* [Good girl.]
    -> Goodbye


=== MinigameMissed ===
Maple sidesteps, ears flattening. Not quite right — she's not settled yet.
+ [Try again.]
    -> StartGroom
* [Leave her for now.]
    -> Goodbye


=== Goodbye ===
-> END
