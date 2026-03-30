EXTERNAL setFlag(key)
EXTERNAL setWorldFlag(key)
EXTERNAL hasFlag(key)
EXTERNAL hasWorldFlag(key)
EXTERNAL giveQuest(questId)
EXTERNAL questActive(questId)
EXTERNAL questCompleted(questId)

-> Intro


=== Intro ===
{ hasFlag("stablemaster.tasksAssigned"): -> IntroRepeat }

"I don't pay you to sleep in Clive."

+ ["Sorry!"]
    "Aye."
    -> Tasks

+ [Say nothing.]
    "Too tired for wittiness this morning?"
    -> Tasks


=== Tasks ===
"Maple's coat is a state. Grab a brush and sort it."

* ["Right, I'll get on it."]
    "Good."
    -> Confirm

* ["Was hoping to take today off."]
    "I'll pretend I didn't hear that!"
    -> Confirm

* ["Anything else?"]
    "Get on then."
    -> Confirm


=== Confirm ===
"You clear on what needs doing?"
-> ConfirmLoop

=== ConfirmLoop ===
+ ["Say it again."]
    "Gods Clive, your ears full of magic dust this morning? Groom Maple. Brush is with Tom if you can't find it."
    "Understood?"
    -> ConfirmLoop

* ["Yeah, I've got it."]
    "Good. Get to it."
    ~ setFlag("stablemaster.tasksAssigned")
    ~ setWorldFlag("stablemaster.hasSpoken")
    ~ giveQuest("GroomMaple")
    -> END


=== IntroRepeat ===
{ hasFlag("stablemaster.morningDone"): -> MorningDone }
"Maple, Clive. Get on with it."

* ["Still on it."]
    -> END

* ["About the guild fee—"]
    "After the work."
    -> END


=== MorningDone ===
"Not bad."

* ["Anything else?"]
    -> AnythingElse

* ["Guild fee?"]
    "End of the week. Keep it up."
    -> END

* [Leave.]
    -> END


=== AnythingElse ===
"Lord Wentworth's man is coming this afternoon."
"Don't talk to him. Don't look at him. Be invisible."

* ["Understood."]
    -> END

* ["Which horse is his?"]
    "The grey in stall two. Don't touch it."
    -> END
