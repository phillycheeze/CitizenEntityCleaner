using System.Text;
using Game.Common;
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
                s_log.Warn($"Error getting corrupted citizen entities: {ex}");
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
        /// Debug Helper
        /// Logs preview list of corrupted citizens (non-delete)
        /// Parameterless overload for Settings UI, sample 10 corrupt entities
        /// </summary>
        public void LogCorruptPreviewToLog() => LogCorruptPreviewToLog(10);

        /// <summary>
        /// Writes up to <paramref name="max"/> truly Corrupt citizens to the log for Scene Explorer checks.
        /// Corrupt (PR1): household has NO PropertyRenter and is NOT homeless/commuter/tourist; skip citizens who are Moving-Away.
        /// </summary>
        public void LogCorruptPreviewToLog(int max)
        {
            if (max <= 0) return;

            try
            {
                // Force Corrupt-only; others OFF. Non-destructive preview.
                using var candidates = GetDeletionCandidates(
                    Allocator.TempJob,
                    tally: false,
                    overrideWantCorrupt: true,
                    overrideWantHomeless: false,
                    overrideWantCommuters: false,
                    overrideWantMovingAwayNoPR: false);

                int count = math.min(max, candidates.Length);
                if (count <= 0)
                {
                    s_log.Info("[Preview] No Corrupt citizens found with the current city data.");
                    return;
                }

                var sb = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append("Corrupt ").Append(FormatIndexVersion(candidates[i]));
                }

                s_log.Info($"[Preview] {sb}");
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"[Preview] Failed: {ex}");
            }
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
