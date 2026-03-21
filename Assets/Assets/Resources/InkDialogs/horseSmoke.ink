EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro


=== Intro ===
{ hasFlag("metSmoke"):
    Smoke shifts restlessly in his stall, ears pinned back. He eyes you sideways.

    * [Try to calm him]
        -> Calm

    * [Keep your distance]
        -> Distance

    * [Leave him alone]
        -> Goodbye

- else:
    ~ setFlag("metSmoke")
    A dark grey stallion, big and restless. He snorts the moment you get close.

    * [Stand your ground]
        -> StandGround

    * [Step back slowly]
        -> StepBack

    * [Leave him alone]
        -> Goodbye
}


=== StandGround ===
Smoke huffs and stomps, but doesn't charge. After a moment he seems to decide you're not worth the effort.

* [Try to calm him]
    -> Calm

* [Leave him alone]
    -> Goodbye


=== StepBack ===
Smart move. Smoke watches you retreat with an air of smug satisfaction.

* [Leave him alone]
    -> Goodbye


=== Calm ===
{ hasFlag("smokeCalmed"):
    You speak low and steady. Smoke's ears flick toward you — he remembers you well enough.

    * [Good.]
        -> Goodbye

- else:
    ~ setFlag("smokeCalmed")
    You speak low, hand raised slow. Smoke's nostrils flare... then he exhales, ears lifting just a fraction. Progress.

    * [There we go.]
        -> Goodbye

    * [Offer something to eat]
        -> OfferFood
}


=== OfferFood ===
{ hasItem("apple", 1):
    ~ giveItem("apple", -1)
    You hold out an apple at arm's length. Smoke stares at it. Then you. Then takes it roughly — but he takes it.

    * [We're getting somewhere.]
        -> Goodbye

- else:
    Nothing to offer. Smoke loses interest immediately.

    * [Maybe next time.]
        -> Goodbye
}


=== Distance ===
Probably wise. Smoke stamps a hoof and goes back to ignoring you.

* [Leave him alone]
    -> Goodbye


=== Goodbye ===

-> END
