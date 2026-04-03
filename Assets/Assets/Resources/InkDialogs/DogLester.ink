EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro


=== Intro ===
{ hasFlag("lester.boneGiven"):
    Lester’s tail starts wagging the moment she sees you. She remembers.

    * [Pet Lester]
        -> PetLester

    * [Good boy.]
        -> Goodbye

- else:
    Lester watches you closely, tail stiff, a low growl rumbling in her throat.

    * [Reach for him]
        -> Bite

    * { hasItem("dog_bone", 1) } ["Easy now... look what I’ve got."]
        -> GiveBone

    * [Back away]
        -> Goodbye
}


=== Bite ===
You reach out slowly.

Lester snaps — quick and sharp. You pull your hand back just in time.

* [Alright... not yet.]
    -> Goodbye

* [Try again]
    -> Bite2


=== Bite2 ===
You try again —

A hand grabs your shoulder and yanks you back.

"Are you mad? Lester’ll take your fingers off."

* [Worth a try.]
    -> Goodbye


=== GiveBone ===
~ giveItem("dog_bone", -1)
~ setFlag("lester.boneGiven")

You toss the bone. Lester catches it, growl melting into eager crunching. Her tail thumps against the dirt.

* [Pet him]
    -> PetLester

* [Good boy.]
    -> Goodbye


=== PetLester ===
You crouch beside Her. Lester leans into your hand, too busy with the bone to mind.

* [That’s a good dog.]
    -> Goodbye


=== Goodbye ===
She lowers her head, eyes drifting closed.
-> END