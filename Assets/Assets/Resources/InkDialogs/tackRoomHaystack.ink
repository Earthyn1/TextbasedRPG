EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
A pile of dry hay stacked against the wall. It smells faintly of dust and horses.

    * [Rummage through it<mg:haystackSearch,2,TimingBar><req:Speed,1>]
        You plunge your hands into the hay...
        ~ startMinigame("haystackSearch,2,TimingBar")
        -> DONE

    * [Leave]
        -> END

=== MinigameFound ===
Your fingers close around something solid. Hidden beneath the hay — a small coin purse!
~ giveItem("potion_mana", 1)
~ setFlag("tackRoom.haystack.searched")

    * [Leave]
        -> END

=== MinigameMissed ===
Nothing but hay and dust. Whatever might have been here, you missed it.

    + [Try again<mg:haystackSearch,2,TimingBar><req:Speed,1>]
        You plunge your hands back into the hay...
        ~ startMinigame("haystackSearch,2,TimingBar")
        -> DONE

    * [Leave]
        -> END
