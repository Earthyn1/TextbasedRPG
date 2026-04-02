EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL removeItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(minigameId)
EXTERNAL reportAction(actionId)
EXTERNAL questActive(questId)


-> Intro

=== Intro ===
Maple turns her head toward you, ears forward, breathing slow and steady.

* { hasItem("brush", 1) && questActive("GroomMaple") } [Brush her down.<mg:brushMaple,1.5,TimedAction,Perception><req:Perception,1>]
    ~ startMinigame("brushMaple, 1.5, TimedAction, Perception, You expertly brush the horse...")
    -> DONE

* { hasItem("potion_mana", 1) } [Offer an apple.]
    -> OfferFood

* { not hasItem("potion_mana", 1) } [Give her a pat.]
    -> Pat

* [Leave her be.]
    -> Goodbye


=== Pat ===
Maple lowers her head slightly as you reach out, warm breath brushing your hand before she leans in.
She leans into your palm — not much, but enough.

* [Good girl.]
    -> Goodbye


=== OfferFood ===
~ removeItem("potion_mana", 1)
You produce an apple. Maple takes it without hesitation, crunching contentedly. Her tail swishes once.

* [There you go.]
    -> Goodbye



=== MinigameFound ===
Long smooth strokes. Maple's coat starts to shine. She exhales slowly — content.

~ removeItem("brush", 1)
~ reportAction("Action_Stable_GroomMaple")

* [Good girl.]
    -> Goodbye


=== Goodbye ===
Maple gives a soft chuff.
-> END