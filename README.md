# Citizen Entity Cleaner

## ⚠️ **WARNING** ⚠️

---

**DO THIS FIRST: BACK UP YOUR SAVE.**  
This mod touches sensitive game components and is a work-around. Use at your own risk.

---


## Problem

Over time, Citizen and Household entities accumulate to extremely large numbers in Cities Skylines 2, far exceeding your actual population. In my example, the save contained 700,000+ citizen entities while only having a population of 40,000. Some users don't have this bug, but others could have millions of corrupted entities.

## Performance Impact

This entity bloat causes several issues:

- **Significant performance drops** due to massive numbers of HouseholdFindSystem pathfinding queries
- **Citizens stuck** at leisure or shopping destinations, never leaving under any circumstance
- **Cars get abandoned** and permanently occupying parking spots, never being used again
- **General performance degradation** across querying systems

ℹ️ **Mod Bonus Options:**
- Delete all commuters (Optional ✅)
- Delete all homeless citizens (Optional ✅)
  
ℹ️ **This mod may also:**
- Delete "Pending" citizens that may be buffered for relocation
- Cause momentary drops in population demand
- Have other long-term consequences not fully understood
- Safe to remove anytime

## Usage

1. **Backup your save file first!**
2. Once the saved city is loaded, go to mod Options.
3. Click [Refresh Counts] to see current statistics.
4. Select checkbox ✅ options as desired.
5. Click [Cleanup Citizens] to clean up entities.

> [!NOTE]
> Revert to original saved city if needed for unexpected behavior.

## What This Mod Does

- Finds households **missing `PropertyRenter`** that still list members
- Marks these citizen entities for deletion in chunked batches
- The base game then naturally cleans up remaining references (households, vehicles, households, vehicles, student/patient references, and anything else afterwards).
- This is a simple mod that doesn't conflict with other mods or overwrite vanilla code/systems, so it is relatively safe in that regard.

> **Does not do**
> - Does **not** delete tourists or citizens in valid households
> - Does **not** run automatically (only acts when you click \[Cleanup Citizens])


## What is causing the issue?

Not sure. It could be another mod or something introduced in a more recent patch. It seems somewhat related to the homeless and leisure bug fixes with the last two patches, so it may be an unintended bug introducted that only is really noticeable after long periods of simulation time. It's possible using Better Bulldozer on any citizens that are homeless can also put them into this state, making the problem worse. Also, if you notice a sudden drop in population when loading up a save and a bunch of households moving back in, its seems like this issue is related to that as well.

## Feedback / Bug Report:
* Go to Discord channel for Citizen Entity Cleaner on **[Cities: Skylines Modding Discord](https://discord.com/channels/1024242828114673724/1402078697120469064)**
  
## Credits
- phillycheese - mod author
- yenyang - feedback, coop development
- Konsi - testing, feedback
- krzychu124 - feedback, code sharing
- Honu - testing, feedback, logo, code updates
