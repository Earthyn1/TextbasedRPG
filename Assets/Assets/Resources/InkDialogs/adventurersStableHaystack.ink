EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
{ hasFlag("adventurersStable.haystack.found"):
    You already searched that!

    * [Leave]
        -> END

    - else:
        A worn blanket half-buried peeks out the side. Someone’s been sleeping here.

       * [Rummage through it<mg:Search,1.5,TimedAction,Perception><req:Perception,1>]
    ~ startMinigame("Search, 1.5, TimedAction, Perception, Shifting through straw..., noXP")
            -> DONE

        * [Leave]
            -> END
}


=== MinigameFound ===
~ setFlag("adventurersStable.haystack.found")
Stuffed deep in the hay — a chewed bone. Lester's been hiding his treasures in here.
~ giveItem("dog_bone", 1)

    * [Pocket it and leave]
        -> END

