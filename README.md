This projects goal is to help familiarise me with C# and to test the viability of an automated card game analysis tool for the popular digital card game STS2.

Controls: (Temporary)

On startup you will be promoted to select a program currently open on your desktop from a drop down menu. Once selected you will be given instructions about what inputs are available and be prompted to enter an input. 

Goals:

The end goal is to snapshot the details of the current game before and after taking an action to see how the action impacted conditions like health score and energy, this would be repeated in identical scenarios with unique actions selected each time in order to determine the most optimal action on a given turn.
Once an optimal action is determined for that scenario, the next scenario begins and the process is repeated. 

This will not determine the optimal action across an entire encounter, only the optimal action in a specific round. I may try to figure that out at some point, but I doubt I will get to that point. (This may be the easier choice once you get to a certain point, as resetting each combat always brings you back to the first round, and I cannot see a simple way to try new actions on the same turn if it is beyond turn 1, without simply taking every previous action the same way again)
I do plan to make this program take into account things like card enchantments and upgrades, this will likely involve integration with the SpireCodex project at some point. 
Initially I will just assume first combat and basic starting deck on the starting character.

Notes:

I probably should have chosen a card game with existing API integration rather than having to rely on a virtual key-press and foreground control, however this game is my obsession of the month so I will just make do.
