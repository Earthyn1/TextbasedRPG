EXTERNAL hasFlag(key)
EXTERNAL setWorldFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL reportAction(actionId)
EXTERNAL questActive(questId)

-> Intro

=== Intro ===
{ hasItem("brush", 1):
    -> AlreadyHave
}
An old horse brush. Well-worn but still good.
* [Take the brush.]
    ~ giveItem("brush", 1)
    ~ setWorldFlag("adventurersStable.brush.taken")
    { questActive("GroomMaple"):
        ~ reportAction("Action_Find_Brush")
    }
    You pocket the brush.
    -> END
* [Leave it.]
    -> END

=== AlreadyHave ===
You already have the brush.
* [Right.]
    -> END
