EXTERNAL hasFlag(key)
EXTERNAL setWorldFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)
EXTERNAL clearWorldFlag(key)

-> Intro

=== Intro ===
        An old horse brush, its seen better days.

        * [Take the brush.]
            ~ giveItem("brush", 1)
            ~ setWorldFlag("adventurersStable.brush.used")
            You pocket the brush.
            -> Test2
            
        * [Return]
            -> END
            
            
=== Test2 ===
         An old horse brush, its seen better days.
         
         *  [Take the brush.]
            ~ giveItem("brush", 1)
            ~ setWorldFlag("adventurersStable.brush.broken")
            You pocket the brush.
            -> Test3
            
=== Test3 ===
        An old horse brush, its seen better days.
        
        *  [Take the brush.]
        ~ giveItem("brush", 1)
        ~ setWorldFlag("adventurersStable.brush.taken")

        You pocket the brush.
        -> Test4


=== Test4 ===
        An old horse brush, its seen better days.
        
        *  [Take the brush.]
        ~ giveItem("brush", 1)
        ~ clearWorldFlag("adventurersStable.brush.taken")
        You pocket the brush.
        -> END

    