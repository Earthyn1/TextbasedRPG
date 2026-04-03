EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL giveXP(skillName, amount)
EXTERNAL startMinigame(id)

-> Intro

=== Intro ===
The goblin lies crumpled in the dirt. A small pouch hangs from its belt.

* { not hasFlag("looted") } [Search the body.<mg:lootGoblin,2,TimedAction,Perception,noXP>]
    ~ startMinigame("lootGoblin, 2, TimedAction, Perception, Searching the body..., noXP")

    -> DONE

* { hasFlag("looted") } [Search the body. <i>(Already looted)</i>]
    Nothing left worth taking.
    -> END

* [Leave it.]
    -> END


=== MinigameFound ===
You rifle through the goblin's pouch and find 10 gold coins.
~ giveItem("gold_coin", 10)
A faint strength settles into your arms.
~ giveXP("Strength", 30)
~ setFlag("looted")

* [Pocket the gold.]
    -> END
