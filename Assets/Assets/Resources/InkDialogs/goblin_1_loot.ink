EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
The goblin lies crumpled in the dirt. A small pouch hangs from its belt.

* { not hasFlag("looted") } [Search the body.<mg:haystackSearch,2,TimedAction,Perception><req:Perception,1>]
    ~ startMinigame("haystackSearch, 2, TimedAction, Perception, Searching the body...")

    -> DONE

* { hasFlag("looted") } [Search the body. <i>(Already looted)</i>]
    Nothing left worth taking.
    -> END

* [Leave it.]
    -> END


=== MinigameFound ===
You rifle through the goblin's pouch and find 10 gold coins.
~ giveItem("gold_coin", 10)
~ setFlag("looted")

* [Pocket the gold.]
    -> END
