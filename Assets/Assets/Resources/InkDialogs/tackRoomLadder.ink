EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro

=== Intro ===
A wooden ladder leads up to the loft above. The rungs are worn smooth from use.

* [Climb up]
    -> StableMasterStop

* [Return]
    -> END


=== StableMasterStop ===
{ hasFlag("tackRoom.ladder.warned"):
    He doesn't even look at you this time. "Don't."
    
    * [Back off]
        -> END

    * [Climb anyway]
        -> ClimbAnyway
- else:
    A firm hand clamps your shoulder before you get far. "Not up there."

    ~ setFlag("tackRoom.ladder.warned")

    * [Alright.]
        -> END

    * [Climb anyway]
        -> ClimbAnyway
}


=== ClimbAnyway ===
He yanks you back down the ladder without effort. "Are you looking to lose work?"

    * [Back off]
        -> END