using System.Text;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CitizenEntityCleaner
{
    // PART: Scan (read-only) — selectors, counts, debug preview, helpers
    public partial class CitizenCleanupSystem : SystemBase
    {
        #region Selection Logic
        /// <summary>
        /// Builds deletion set based on toggles (or overrides for debug preview):
        /// - Corrupt households (no PropertyRenter & not homeless/commuter/tourist) - excludes members who are Moving-Away
        /// - HomelessHousehold members when IncludeHomeless == true
        /// - CommuterHousehold members when IncludeCommuters == true
        /// - Moving-Away (no PropertyRenter) when IncludeMovingAwayNoPR == true
        ///
        /// Notes:
        /// • Optional overrides let callers force a specific combo (e.g., Debug preview runs "Corrupt-only" regardless of checkboxes).
        /// • If "tally" = true, method increments per-category counters in "m_lastCounts" as it builds the candidate list.
        ///     Set tally = false only when need the list/count (e.g., UI refresh or preview).
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


            // Mirror UI defaults: Corrupt defaults ON; others default OFF
            // Order: override → UI settings → fallback
            bool wantCorrupt = ResolveToggle(overrideWantCorrupt, m_settings?.IncludeCorrupt, fallback: true);
            bool wantHomeless = ResolveToggle(overrideWantHomeless, m_settings?.IncludeHomeless, fallback: false);
            bool wantCommuters = ResolveToggle(overrideWantCommuters, m_settings?.IncludeCommuters, fallback: false);
            bool wantMovingAwayNoPR = ResolveToggle(overrideWantMovingAwayNoPR, m_settings?.IncludeMovingAwayNoPR, fallback: false);


            // Early-out: nothing selected
            if (!wantCorrupt && !wantHomeless && !wantCommuters && !wantMovingAwayNoPR)
                return candidates;

            try
            {

                using var processedHouseholds =
                    new NativeHashSet<Entity>(math.max(1, householdMembers.Length), Allocator.TempJob);

                foreach (var householdMember in householdMembers)
                {
                    Entity householdEntity = householdMember.m_Household;

                    // Skip if missing or already processed
                    if (!EntityManager.Exists(householdEntity)) continue;
                    if (!processedHouseholds.Add(householdEntity)) continue;

                    // Must have members buffer, skip households without members list
                    if (!EntityManager.HasBuffer<HouseholdCitizen>(householdEntity)) continue;

                    // Household category flags
                    bool hasPropertyRenter = EntityManager.HasComponent<PropertyRenter>(householdEntity);
                    bool isHomelessHH = EntityManager.HasComponent<HomelessHousehold>(householdEntity);
                    bool isCommuterHH = EntityManager.HasComponent<CommuterHousehold>(householdEntity);
                    bool isTouristHH = EntityManager.HasComponent<TouristHousehold>(householdEntity);

                    // Quick HH-level skip optimization: if nothing in this HH could match
                    // and the independent Moving-Away rule is OFF, skip members.
                    bool isResidentCorrupt = !hasPropertyRenter && !isHomelessHH && !isCommuterHH && !isTouristHH;
                    bool householdMatchesAny =
                        (wantHomeless && isHomelessHH) ||
                        (wantCommuters && isCommuterHH) ||
                        (wantCorrupt && isResidentCorrupt);

                    if (!householdMatchesAny && !wantMovingAwayNoPR)
                        continue;

                    // Iterate members and apply per-citizen rules (via shared classifier)
                    var householdCitizens = EntityManager.GetBuffer<HouseholdCitizen>(householdEntity);
                    for (int j = 0; j < householdCitizens.Length; j++)
                    {
                        var citizenEntity = householdCitizens[j].m_Citizen;
                        if (!EntityManager.Exists(citizenEntity)) continue;
                        if (EntityManager.HasComponent<Deleted>(citizenEntity)) continue;

                        var reason = ClassifyCitizenForDeletion(
                            wantCorrupt, wantHomeless, wantCommuters, wantMovingAwayNoPR,
                            isHomelessHH, isCommuterHH, isTouristHH, hasPropertyRenter,
                            citizenEntity);

                        if (reason != CleanupType.None)
                        {
                            // mark for deletion; only update tallies when a real cleanup is run
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
        /// Count citizens to clean based on toggles:
        /// IncludeCorrupt, IncludeHomeless, IncludeCommuters.
        /// </summary>
        private int GetCitizensToCleanCount()
        {
            using var candidates = GetDeletionCandidates(Allocator.TempJob, tally: false);
            return candidates.Length;
        }
        #endregion

        #region Debug Helpers
        // ---- Debug Log: preview only, no delete ----
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

            s_Log.Info($"[Preview] ==== Corrupt sample (up to {count}) ====");

            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("Corrupt ").Append(FormatIndexVersion(candidates[i]));
            }

            s_Log.Info($"[Preview] {sb}");
        }

        #endregion

        #region Helpers
        // Resolve boolean: precedence: override → UI setting → fallback default.
        // Example: ResolveToggle(forced:true,  setting:false, fallback:false) => true
        //          ResolveToggle(forced:null,  setting:true,  fallback:false) => true
        //          ResolveToggle(forced:null,  setting:null,  fallback:true)  => true
        private static bool ResolveToggle(bool? overrideValue, bool? settingValue, bool fallback)
        {
            return overrideValue ?? (settingValue ?? fallback);
        }

        // Formats an Entity as "Index:Version" for Scene Explorer cross-checks and logs.
        private static string FormatIndexVersion(Entity e) => $"{e.Index}:{e.Version}";


        // Returns true if the citizen is in a Moving-Away state.
        // Checks the tag component first; falls back to TravelPurpose if present.
        private bool IsMovingAway(Entity citizenEntity)
        {
            // Primary
            if (EntityManager.HasComponent<MovingAway>(citizenEntity))
                return true;

            // Fallback
            if (EntityManager.HasComponent<TravelPurpose>(citizenEntity))
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

        #endregion
    }
}
