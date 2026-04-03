EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
A pile of dry hay...

   * [Rummage through it<mg:goblinSearch,1.5,TimedAction,Perception><req:Perception,1>]
    ~ startMinigame("goblinSearch, 1.5, TimedAction, Perception, Searching the pile of hay..., noXP")
        -> DONE

    * [Leave]
        -> END

=== MinigameFound ===
Your fingers close around something solid. A single Coin...
~ giveItem("gold_coin", 1)
~ setFlag("tackRoom.haystack.searched")

    * [Leave]
        -> END


