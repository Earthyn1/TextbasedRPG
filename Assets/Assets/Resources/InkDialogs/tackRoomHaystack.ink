EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
A pile of dry hay stacked against the wall. It smells faintly of dust and horses.

   * [Rummage through it<mg:haystackSearch,2,TimedAction,Perception><req:Perception,1>]
    ~ startMinigame("haystackSearch, 2, TimedAction, Perception, Searching the stall...")
        -> DONE

    * [Leave]
        -> END

=== MinigameFound ===
Your fingers close around something solid. Hidden beneath the hay — a small coin purse!
~ giveItem("potion_mana", 1)
~ setFlag("tackRoom.haystack.searched")

    * [Leave]
        -> END


