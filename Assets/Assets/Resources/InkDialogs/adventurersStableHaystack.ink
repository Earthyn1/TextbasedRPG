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
    { hasFlag("adventurersStable.haystack.failed"):
        Before you can dig in again, Tom's voice cuts across the stall. "Oi, leave that alone!"

        * [Back off]
            -> END

    - else:
        A tall pile of hay stuffed into the corner of the stall. Someone's bedded down here recently — there's a worn blanket half-buried in it.

        * [Rummage through it<mg:haystackMemory,1,Memory,dog_bone><req:Perception,1>]

            ~ startMinigame("haystackMemory,1,Memory,dog_bone")
            -> DONE

        * [Leave]
            -> END
    }
}

=== MinigameFound ===
~ setFlag("adventurersStable.haystack.found")
Stuffed deep in the hay — a chewed bone. Lester's been hiding his treasures in here.
~ giveItem("dog_bone", 1)

    * [Pocket it and leave]
        -> END

=== MinigameMissed ===
~ setFlag("adventurersStable.haystack.failed")
Hay, dust, and the smell of horse. If there's something here, you couldn't find it.

    * [Leave]
        -> END
