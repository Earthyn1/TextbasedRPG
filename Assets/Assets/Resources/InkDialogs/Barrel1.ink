EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro

=== Intro ===
        The barrel is filled with a dark liquid... smells odd too.

      
        
        * { hasItem("fishingRod", 1) } [Lets go fishing!]
            -> FishingSuccess

        * { not hasItem("fishingRod", 1) } [<alpha=\#99>Lets go fishing! (<i>Rusty Key</i>)]

            -> FishingFail
            
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
    