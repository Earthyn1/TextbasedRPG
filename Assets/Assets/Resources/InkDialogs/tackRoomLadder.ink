EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro

=== Intro ===
A wooden ladder leads up to the loft above. The rungs look worn but sturdy enough.

    * [Climb up]
        -> StableBoyWarning

    * [Return]
        -> END

=== StableBoyWarning ===
{ hasFlag("tackRoom.ladder.warned"):
    The stableboy eyes you sharply. "Don't do this again."

    * [Back off]
        -> END
- else:
    The stableboy yells "Oi! You're not allowed up there!"

    * [Climb anyway]
        -> ClimbAnyway

    * [Leave]
        -> END
}

=== ClimbAnyway ===
~ setFlag("tackRoom.ladder.warned")
He rushes over and pulls you off the ladder angrily. "Are you deaf??"

    * [Back off]
        -> END
