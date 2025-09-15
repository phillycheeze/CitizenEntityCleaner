using Game.Common; // for Deleted

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CitizenEntityCleaner
{
    // PART: Scan (read-only) — candidate lists, classify & filter, read-only counts, preview-only logs
    public partial class CitizenCleanupSystem
    {
        /// <summary>
        /// Gets all corrupted citizen entities (those in households with no PropertyRenter)
        /// Applies added filters based on settings for homeless and commuters
        /// </summary>
        private NativeList<Entity> GetCorruptedCitizenEntities(Allocator allocator)
        {
            var corruptedCitizens = new NativeList<Entity>(allocator);

            try
            {
                using var householdMembers = m_householdMemberQuery.ToComponentDataArray<Game.Citizens.HouseholdMember>(Allocator.TempJob);
                // Create a hash set with capacity equal to number of household members, but never less than 1
                using var processedHouseholds =
                    new NativeHashSet<Entity>(math.max(1, householdMembers.Length), Allocator.TempJob);

                foreach (var householdMember in householdMembers)
                {
                    Entity householdEntity = householdMember.m_Household;

                    // Skip if already processed or doesn't exist
                    if (processedHouseholds.Contains(householdEntity) || !EntityManager.Exists(householdEntity))
                        continue;

                    processedHouseholds.Add(householdEntity);

                    // Check if household is corrupted (no PropertyRenter component)
                    if (!EntityManager.HasComponent<Game.Buildings.PropertyRenter>(householdEntity) &&
                        EntityManager.HasBuffer<Game.Citizens.HouseholdCitizen>(householdEntity))
                    {
                        var householdCitizens = SystemAPI.GetBuffer<Game.Citizens.HouseholdCitizen>(householdEntity);

                        foreach (var householdCitizen in householdCitizens)
                        {
                            Entity citizenEntity = householdCitizen.m_Citizen;

                            if (EntityManager.Exists(citizenEntity) && !EntityManager.HasComponent<Deleted>(citizenEntity))
                            {
                                if (ShouldIncludeCitizen(citizenEntity))
                                {
                                    corruptedCitizens.Add(citizenEntity);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting corrupted citizen entities: {ex.Message}");
            }

            return corruptedCitizens;
        }

        /// <summary>
        /// Gets count of corrupted citizens
        /// </summary>
        private int GetCorruptedCitizenCount()
        {
            using var corruptedCitizens = GetCorruptedCitizenEntities(Allocator.TempJob);
            return corruptedCitizens.Length;
        }

        /// <summary>
        /// Determines whether a citizen should be included in cleanup based on filter settings
        /// </summary>
        private bool ShouldIncludeCitizen(Entity citizenEntity)
        {
            var settings = m_settings;
            if (settings == null) return true;

            // Check commuter (exclude if commuter and IncludeCommuters is false)
            if (EntityManager.HasComponent<Game.Citizens.Citizen>(citizenEntity))
            {
                var citizen = EntityManager.GetComponentData<Game.Citizens.Citizen>(citizenEntity);
                if ((citizen.m_State & Game.Citizens.CitizenFlags.Commuter) != 0 && !settings.IncludeCommuters)
                    return false;
            }

            // Check homeless (exclude if homeless and IncludeHomeless is false)
            if (EntityManager.HasComponent<Game.Citizens.CurrentTransport>(citizenEntity))
            {
                var transport = EntityManager.GetComponentData<Game.Citizens.CurrentTransport>(citizenEntity);
                var human = transport.m_CurrentTransport;

                // Guard: ensure Human exists and has the component before reading it
                if (EntityManager.Exists(human) && EntityManager.HasComponent<Game.Creatures.Human>(human))
                {
                    var humanData = EntityManager.GetComponentData<Game.Creatures.Human>(human);
                    if ((humanData.m_Flags & HomelessFlag) != 0 && !settings.IncludeHomeless)
                        return false;
                }
            }

            return true;
        }
    }
}
