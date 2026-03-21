EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
        The barrel is filled with a dark liquid... smells odd too.



        * { hasItem("fishingRod", 1) } [Lets go fishing!]
            -> FishingSuccess

        * { not hasItem("fishingRod", 1) } [<alpha=\#99>Lets go fishing! (<i>Rusty Key</i>)]

            -> FishingFail

        * [Inspect the liquid<mg:barrelAether,35,Aether><req:Aethur,3>]
            You peer into the dark liquid, tracing the strange patterns swirling within...
            ~ startMinigame("barrelAether,35,Aether")
            -> DONE

        * [Return]
            -> END


    === FishingSuccess ===
    You managed to fish in this barrel??

        * [Leave]
            -> END


    === FishingFail ===
    I feel like I am missing something...

        * [Leave]
            -> END

=== MinigameFound ===
The patterns resolve into something clear — a ward, etched into the liquid itself. You read it perfectly.

    * [Step back]
        -> END

=== MinigameMissed ===
The swirling patterns blur and scatter. Whatever was written there, you couldn't hold it long enough to read.

    * [Step back]
        -> END
