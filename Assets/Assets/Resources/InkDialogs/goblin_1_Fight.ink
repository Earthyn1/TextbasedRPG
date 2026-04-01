EXTERNAL setFlag(key)
EXTERNAL setWorldFlag(key)
EXTERNAL hasFlag(key)
EXTERNAL hasWorldFlag(key)
EXTERNAL startCombat(enemyId)

-> Intro

=== Intro ===
A goblin blocks your path, teeth bared and rusty blade drawn.

* [Attack!]
    ~ startCombat("goblin1")
    -> DONE

* [Try to sneak past.]
    The goblin spots you immediately.
    ~ startCombat("goblin1")
    -> DONE


=== CombatWon ===
The goblin crumples to the dirt. You stand over it, catching your breath.

* [Search the body.]
    Nothing worth taking.
    -> END

* [Move on.]
    -> END


=== CombatLost ===
Everything goes dark...

* [...]
    -> END
