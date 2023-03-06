# Downpour: Fighting Time

![Remember that as time increases, so does difficulty.](https://cdn.discordapp.com/attachments/567832879879553037/1075841504834224239/image.png)

> Also known as "Woolie's Paradise" by nobody

Downpour is a difficulty mod that reworks the current leveling system to encourage fast-paced gameplay. The goal of the mod is to make the early-game easier while farming gold for items and staying ahead of the curve a choice players have to make constantly. Remember, **leaving chests behind** is a viable strategy in Downpour. you will have to give up some loot to stay ahead of the scale.

Also changes simulacrum to be much faster paced. Aim for wave 50!

+Also changes gup to be less tanky but more dangerous on split.

## How it works
<a href="https://www.youtube.com/watch?v=W0VlysVaudI">![Video](https://media.discordapp.net/attachments/515678821408571392/1077735348014153798/RoR2__Downpour_Difficulty_Scale_Comparison_0-24_screenshot.png?width=1098&height=618)</a>

(Click for video)

## How it works (for nerds)
<details><summary>(Math inbound)</summary>
Lets look at vanilla scaling first.

![Vanilla Scaling Function](https://cdn.discordapp.com/attachments/515678914316861451/1075613757180481656/image.png)

It looks daunting at first, but to point out the important parts:
- Every stage advancement multiplies the total level by 1.15.
- Difficulty scales linearly based on time.
- Therefore, stage number becomes crucially important, while how much you spend in each stage does not matter as much.
- Level stops scaling at 99.

This leads to full clearing the stage being objectively more beneficial than rushing, players often do not have to think whether to move fast or farm money for items to stay ahead of the curve. Also, since stage advancement is the only exponential source, you outscale the enemies easily after the first 5 stages. This makes looping feel boring often. With the level cap, this problem is exacerbated.

Now, let's look at vanilla's Simulacrum scaling.

![Vanilla Simulacrum Scaling Function](https://cdn.discordapp.com/attachments/515678914316861451/1075614653444534322/image.png)

This one does not take players or time into account at all. This makes some sense since Simulacrum wasn't meant to be played with people. Anyways, neither of the scaling functions are fast enough to keep up with the exponentially scaling player.

Okay, time for Downpour's changes. starting with normal runs...

![Downpour Scaling Function](https://cdn.discordapp.com/attachments/515678914316861451/1075619161855758407/image.png) 

Even scarier, but here's the important part:
- Time now scales exponentially, the position of stage and time has been essentially swapped.
- Time scaling is split into two kinds of scaling: Permanent and Temporary. temporary scaling is reset every time you enter a new stage.
- Stage scaling is now linear, and adds onto the difficulty. in vanilla setting, every difficulty starts easier on the first stage, but scaling is faster than vanilla on fourth and onwards.
- Player scaling is less, though more multiplayer testing is needed for this.

Generally you're expected to keep up with the permanent scaling, denoted by "Exponential Difficulty Scaling" in the game. Don't be discouraged if you fall behind on Downpour (difficulty) though, it's meant to do that.

Finally, Simulacrum.

![Simulacrum Downpour Scaling Function](https://media.discordapp.net/attachments/515678914316861451/1075802566190968932/image.png)

With the fast simulacrum changes and by making it use the default scaling function with modifiers, Difficulties feel just like the one from the base game, making it more seamless to play. All values with "Scaling" on it is configurable.
</details>

## Other simulacrum changes
- Once all enemies are defeated, more enemies will be insta-spawned, so you can speed up the waves.
- Countdown between waves are configurable, and by default lowered to 3 seconds. (No countdown for Downpour)
- Waves will slowly release faster on its own, from 30 seconds to 3 (maxed out at level 31).

## Recommended mods to play with
- [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/): Adds in-game tweaking of all scaling values the mod provides.
- [Inferno](https://thunderstore.io/package/HIFU/Inferno/): Adds a special "Inferno Downpour" difficulty that is a fusion between Downpour and Inferno.
- [FasterBossWait2](https://thunderstore.io/package/prodzpod/FasterBossWait2/): Reduces holdout time without removing it outright. Mod is balanced around having this.
- [ResumeMusicPostTeleporter](https://thunderstore.io/package/prodzpod/ResumeMusicPostTeleporter/): Resumes stage music after the teleporter event has been completed. If you're rushing, you'll encounter a lot of silence.
- [BetterMoonPillars](https://thunderstore.io/package/prodzpod/BetterMoonPillars/): Reworks moon pillars to be optional but rewards you with items and time. Makes the mithrix fight easier without looping.
- [LittleGameplayTweaks](https://thunderstore.io/package/Wolfo/LittleGameplayTweaks/): Set `4ba - Simulacrum Ending > Ending Start Wave` and `4bb - Simulacrum Forced Boss > Forced Special Boss Start Wave` to `50` for a finite simulacrum experience.
- [ConsistentStageFeatures](https://thunderstore.io/package/prodzpod/ConsistentStageFeatures/): Makes runs a bit more reliable.
- [ProperLoop](https://thunderstore.io/package/prodzpod/ProperLoop/): Stops Vields cheese.
- [BossAntiSoftlock](https://thunderstore.io/package/JustDerb/BossAntiSoftlock/): Removes unfair situations where the boss is too far away for you to hit.

## Compatibility Stuff
- [Raise Monster Level Cap](https://thunderstore.io/package/Moffein/Raise_Monster_Level_Cap/): Downpour already uncaps level. Having this mod will only apply to stuff you've disabled in Downpour's config.
- [LittleGameplayTweaks](https://thunderstore.io/package/Wolfo/LittleGameplayTweaks/): Downpour will override its simulacrum scaling changes.
- [WellRoundedBalance](https://thunderstore.io/package/TheBestAssociatedLargelyLudicrousSillyheadGroup/WellRoundedBalance/): Disable `Mechanics : Scaling` from WRB settings if you want Downpour scaling on non-downpour difficulties. Simulacrum will still be governed by Downpour settings.

## Changelog
- 1.0.7: proper [WellRoundedBalance](https://thunderstore.io/package/TheBestAssociatedLargelyLudicrousSillyheadGroup/WellRoundedBalance/) support.
- 1.0.6: fixed it just Not Working
- 1.0.5: updated compat, nerfed gup
- 1.0.4: Added simulacrum boss health multiplier before wave 50 (forgot)
- 1.0.3: Stops vfx after level 100 to prevent lag/sfx spam
- 1.0.2: Bugfix, nerfed simulacrum further to RMB levels
- 1.0.1: IL is no longer hijacked, WRB "compat", Default Simulacrum scaling nerfed, better README, fixed description inaccuracy with some modded difficulties
- 1.0.0: Initial Release.