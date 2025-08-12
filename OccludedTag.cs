using Unity.Entities;

namespace CitizenEntityCleaner
{
    // Tag all occluded entities so we can exclude them from search‐tree queries without per‐entity churn
    public struct OccludedTag : IComponentData, IEnableableComponent
    {
        // All instances compare equal
        public bool Equals(OccludedTag other) => true;
        public override bool Equals(object obj) => obj is OccludedTag;
        public override int GetHashCode() => 0;
    }
}
