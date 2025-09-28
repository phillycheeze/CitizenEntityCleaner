using System.Text;
using Game.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Buildings;     // PropertyRenter
using Game.Citizens;      // HouseholdMember, HouseholdCitizen, HomelessHousehold, CommuterHousehold, TouristHousehold, TravelPurpose
using Game.Agents;        // MovingAway

namespace CitizenEntityCleaner
{
    // PART: Scan (read-only) — candidate lists, classify & filter, read-only counts, preview-only logs
    public partial class CitizenCleanupSystem
    {

        /// <summary>
        /// selector: builds candidate list based on toggles (or overrides for preview).
        /// Corrupt = !PropertyRenter && !Homeless && !Commuter && !Tourist; skip Moving-Away for Corrupt.
        /// </summary>
        private NativeList<Entity> GetDeletionCandidates(
            Allocator allocator,
            bool tally = true,
            bool? overrideWantCorrupt = null,
            bool? overrideWantHomeless = null,
            bool? overrideWantCommuters = null,
            bool? overrideWantMovingAwayNoPR = null)
        {
            using var householdMembers = m_householdMemberQuery.ToComponentDataArray<HouseholdMember>(Allocator.TempJob);
            var candidates = new NativeList<Entity>(math.max(1, householdMembers.Length), allocator);

            // Defaults: Corrupt ON; others OFF (override → setting → fallback)
            bool wantCorrupt = ResolveToggle(overrideWantCorrupt, m_settings?.IncludeCorrupt, fallback: true);
            bool wantHomeless = ResolveToggle(overrideWantHomeless, m_settings?.IncludeHomeless, fallback: false);
            bool wantCommuters = ResolveToggle(overrideWantCommuters, m_settings?.IncludeCommuters, fallback: false);
            bool wantMovingAwayNoPR = ResolveToggle(overrideWantMovingAwayNoPR, m_settings?.IncludeMovingAwayNoPR, fallback: false);

            if (!wantCorrupt && !wantHomeless && !wantCommuters && !wantMovingAwayNoPR)
                return candidates;

            try
            {
                using var processedHouseholds =
                    new NativeHashSet<Entity>(math.max(1, householdMembers.Length), Allocator.TempJob);

                foreach (var householdMember in householdMembers)
                {
                    var householdEntity = householdMember.m_Household;

                    if (!EntityManager.Exists(householdEntity)) continue;
                    if (!processedHouseholds.Add(householdEntity)) continue;
                    if (!EntityManager.HasBuffer<HouseholdCitizen>(householdEntity)) continue;

                    bool hasPR = EntityManager.HasComponent<PropertyRenter>(householdEntity);
                    bool isHomelessHH = EntityManager.HasComponent<HomelessHousehold>(householdEntity);
                    bool isCommuterHH = EntityManager.HasComponent<CommuterHousehold>(householdEntity);
                    bool isTouristHH = EntityManager.HasComponent<TouristHousehold>(householdEntity);

                    bool isResidentCorrupt = !hasPR && !isHomelessHH && !isCommuterHH && !isTouristHH;
                    bool householdMatchesAny =
                        (wantHomeless && isHomelessHH) ||
                        (wantCommuters && isCommuterHH) ||
                        (wantCorrupt && isResidentCorrupt);

                    if (!householdMatchesAny && !wantMovingAwayNoPR)
                        continue;

                    var householdCitizens = EntityManager.GetBuffer<HouseholdCitizen>(householdEntity);
                    for (int j = 0; j < householdCitizens.Length; j++)
                    {
                        var citizenEntity = householdCitizens[j].m_Citizen;
                        if (!EntityManager.Exists(citizenEntity)) continue;
                        if (EntityManager.HasComponent<Deleted>(citizenEntity)) continue;

                        var reason = ClassifyCitizenForDeletion(
                            wantCorrupt, wantHomeless, wantCommuters, wantMovingAwayNoPR,
                            isHomelessHH, isCommuterHH, isTouristHH, hasPR,
                            citizenEntity);

                        if (reason != CleanupType.None)
                        {
                            candidates.Add(citizenEntity);
                            if (tally) m_lastCounts.BumpCount(reason);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                s_Log.Warn($"Error building deletion candidates list: {ex.Message}");
            }

            return candidates;
        }

        /// <summary>
        /// Count citizens to clean based on toggles: IncludeCorrupt, IncludeHomeless, IncludeCommuters, IncludeMovingAwayNoPR.
        /// </summary>
        private int GetCitizensToCleanCount()
        {
            using var candidates = GetDeletionCandidates(Allocator.TempJob, tally: false);
            return candidates.Length;
        }

        // ---- HELPERS ----

        /// <summary>
        /// Debug Helper
        /// Logs preview list of corrupted citizens (non-delete)
        /// </summary>
        public void LogCorruptPreviewToLog(int max)
        {
            if (max <= 0) return;

            // Reuse the same traversal, force Corrupt=true and others=false; no state changes.
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
                s_Log.Info("[Preview] No Corrupt citizens found with the current city data.");
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("Corrupt ").Append(FormatIndexVersion(candidates[i]));
            }

            s_Log.Info($"[Preview] {sb}");
        }

        // Resolves boolean: precedence override → setting → fallback default.
        private static bool ResolveToggle(bool? overrideValue, bool? settingValue, bool fallback)
            => overrideValue ?? (settingValue ?? fallback);

        // Formats & logs an Entity as "Index:Version" for Scene Explorer cross-checks.
        private static string FormatIndexVersion(Entity e) => $"{e.Index}:{e.Version}";

        // Returns true if the citizen is Moving-Away
        private bool IsMovingAway(Entity citizenEntity)
        {
            if (EntityManager.HasComponent<MovingAway>(citizenEntity))  // Checks tag component first
                return true;

            if (EntityManager.HasComponent<TravelPurpose>(citizenEntity))   // Fallback - TravelPurpose
            {
                var tp = EntityManager.GetComponentData<TravelPurpose>(citizenEntity);
                if (tp.m_Purpose == Purpose.MovingAway)
                    return true;
            }
            return false;
        }

        private CleanupType ClassifyCitizenForDeletion(
            bool wantCorrupt,
            bool wantHomeless,
            bool wantCommuters,
            bool wantMovingAwayNoPR,
            bool isHomelessHH,
            bool isCommuterHH,
            bool isTouristHH,
            bool hasPropertyRenter,
            Entity citizenEntity)
        {
            bool movingAway = IsMovingAway(citizenEntity);

            // Precedence: Homeless → Commuters → Corrupt, then independent Moving-Away (no PR)
            if (wantHomeless && isHomelessHH) return CleanupType.Homeless;
            if (wantCommuters && isCommuterHH) return CleanupType.Commuters;

            if (wantCorrupt && !hasPropertyRenter && !isHomelessHH && !isCommuterHH && !isTouristHH)
            {
                // Skip corrupt if the person is Moving-Away
                if (!movingAway) return CleanupType.Corrupt;
            }

            if (wantMovingAwayNoPR && movingAway && !hasPropertyRenter) return CleanupType.MovingAway;

            return CleanupType.None;
        }

    }
}
