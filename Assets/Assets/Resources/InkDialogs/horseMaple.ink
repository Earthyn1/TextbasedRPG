EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro


=== Intro ===
{ hasFlag("metMaple"):
    Maple turns her head toward you, ears forward, breathing slow and steady.

    * [Give her a pat]
        -> Pat

    * [Offer something to eat]
        -> OfferFood

    * [Leave her be]
        -> Goodbye

- else:
    ~ setFlag("metMaple")
    A dappled mare with a white blaze down her nose. She watches you approach with calm, dark eyes.

    * [Hold out your hand]
        -> Sniff

    * [Give her a pat]
        -> Pat

    * [Leave her be]
        -> Goodbye
}


=== Sniff ===
Maple lowers her muzzle to your palm, huffing warm breath across your fingers. She seems satisfied.

* [Give her a pat]
    -> Pat

* [Leave her be]
    -> Goodbye


=== Pat ===
She leans into your hand just slightly — not much, but enough.

* [Good girl.]
    -> Goodbye

* [Offer something to eat]
    -> OfferFood


=== OfferFood ===
{ hasItem("apple", 1):
    ~ giveItem("apple", -1)
    You produce an apple. Maple takes it without hesitation, crunching contentedly. Her tail swishes once.

    * [There you go.]
        -> Goodbye

- else:
    You've got nothing on you worth offering. Maple sniffs your empty hand and turns away, unimpressed.

    * [Fair enough.]
        -> Goodbye
}


=== Goodbye ===

-> END
