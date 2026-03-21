EXTERNAL hasFlag(key)
EXTERNAL setFlag(key)
EXTERNAL giveItem(itemId, count)
EXTERNAL hasItem(itemId, count)

-> Intro


=== Intro ===
{ hasFlag("lester.boneGiven"):
    Lester’s tail wags the moment he sees you. He remembers.

    * [Pet Lester]
        -> PetLester

    * [Good boy.]
        -> Goodbye

- else:
    Lester eyes you warily, tail twitching, a low growl rumbling in his throat.

    * [Pet Dog]
        -> Bite

    * { hasItem("dog_bone", 1) } [<i><color=\#228B22>Easy now, boy... look what I’ve got.</color> (Bone)</i>]
        -> GiveBone

    * { not hasItem("dog_bone", 1) } [<alpha=\#99>Easy now, boy... look what I’ve got. (<i>Bone</i>)]
        -> NeedBone

    * [Back away from the dog]
        -> Goodbye
}
    


=== Bite ===
You approach Lester... stupidly. Lester snaps at you as you quickly pull back your hand.

* [Woah, might need to earn his trust]
    -> Goodbye

* [Try again]
    -> Bite2
    
    === Bite2 ===
You approach Lester again... but someone pulls you back "Are you mad?? Lester don't like you!"

* [Worth a try]
    -> Goodbye

=== NeedBone ===
You pat your pockets. Nothing. Lester growls louder.
    * [Return]
    -> Intro



=== GiveBone ===
~ giveItem("dog_bone", -1)
~ setFlag("lester.boneGiven")

      You toss Lester the bone. His tail thumps the dirt, growl fading into eager crunching.

       * [Try to pet Lester]
        -> PetLester
        
      * [Good boy.]
        -> Goodbye
    


=== PetLester === 
You aggressivly pet Lester's head and belly as he gnaws on the bone too distracted to notice.

 * [Thats a good dog.]
        -> Goodbye


=== Goodbye ===

-> END


