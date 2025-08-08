# Citizen Entity Cleaner

## ⚠️ **WARNING** ⚠️

**<span style="color:red">This is a hacky workaround mod that touches sensitive game components. Backup your saved game before using this mod!</span>**

## Problem

Over time, Citizen and Household entities accumulate to extremely large numbers in Cities Skylines 2, far exceeding your actual population. In my example, the save contained 700,000+ citizen entities while only having a population of 40,000.

## Performance Impact

This entity bloat causes several issues:

- **Significant performance drops** due to massive numbers of HouseholdFindSystem pathfinding queries
- **Citizens stuck** at leisure or shopping destinations, never leaving under any circumstance
- **Cars get abandoned** and permanently occupying parking spots, never being used again
- **General performance degradation** across querying systems

## What This Mod Does

Deletes any Citizen entites that don't have a PropertyRenter component attached to it. The vanilla game will clean up the households, vehicles, and anything else afterwards. It is a very simple mod that doesn't conflict with other mods or overwrite vanilla code/systems, so it is relatively safe in that regard.

## Unintended Consequences

⚠️ **This mod will also delete:**
- All homeless citizens and households
- "Pending" citizens that may be buffered for relocation
- May cause momentary drops in population demand
- May have other long-term consequences not fully understood

## Usage

1. **Backup your save file first!**
2. Open mod settings in-game
3. Click "Refresh Counts" to see current statistics
4. Click "Cleanup Corrupted Citizens" to clean up entities
5. Monitor your city for any unexpected behavior


## What is causing the issue?

Not sure. It could be another mod or something introduced in a more recent patch. It seems somewhat related to the homeless and leisure bug fixes with the last two patches, so it may be an unintended bug introducted that only is really noticeable after long periods of simulation time. It's possible using Better Bulldozer on any citizens that are homeless can also put them into this state, making the problem worse. Also, if you notice a sudden drop in population when loading up a save and a bunch of households moving back in, its seems like this issue is related to that as well.

## Thanks to
yenyang
Konsi
krzychu124
Honu
