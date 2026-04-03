EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro


=== Intro ===
{ hasFlag("smokeCalmed"):
    Smoke shifts but doesn’t flare this time. One ear flicks toward you.

    * [Stay calm]
        -> Calm

    * [Leave him]
        -> Goodbye

- else:
    A dark grey stallion, restless and tense. Smoke snorts as you approach, ears pinned.

    * [Hold steady]
        -> Calm

    * [Back off]
        -> Goodbye
}


=== Calm ===
{ hasFlag("smokeCalmed"):
    You keep your voice low. Smoke watches you, wary — but not pushing.

    * [Good.]
        -> Goodbye

- else:
    ~ setFlag("smokeCalmed")
    You move slow, voice low. Smoke snorts — then exhales, tension easing just a little.

    * [Easy...]
        -> AfterCalm

    * { hasItem("apple", 1) } [Offer an apple]
        -> OfferFood
}


=== AfterCalm ===
Smoke shifts his weight, still watching you closely.

* [Leave him]
    -> Goodbye


=== OfferFood ===
~ giveItem("apple", -1)

You hold the apple out carefully. Smoke hesitates — then snatches it, chewing hard.

* [That’s it.]
    -> Goodbye


=== Goodbye ===
Smoke snorts softly, watching you as you step away.
-> END