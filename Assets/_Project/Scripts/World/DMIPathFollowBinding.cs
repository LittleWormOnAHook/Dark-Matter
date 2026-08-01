using UnityEngine;
using MalbersAnimations.PathCreation;

namespace Project.World
{
    /// <summary>
    /// Resolves a Path Creator (or Path Creator Variant root) to a
    /// <see cref="DMIPathFollowProvider"/> that exposes bezier anchors for patrol / path-follow.
    /// </summary>
    public static class DMIPathFollowBinding
    {
        /// <summary>
        /// Finds an existing provider on the Path Creator GameObject, or adds one so AI can register.
        /// </summary>
        public static DMIPathFollowProvider Resolve(PathCreator pathCreator, bool addIfMissing = true)
        {
            if (pathCreator == null)
                return null;

            DMIPathFollowProvider provider = pathCreator.GetComponent<DMIPathFollowProvider>();
            if (provider != null)
                return provider;

            if (!addIfMissing)
                return null;

            provider = pathCreator.gameObject.AddComponent<DMIPathFollowProvider>();
            return provider;
        }

        /// <summary>
        /// Accepts PathCreator, DMIPathFollowProvider, or any component/GameObject that has either.
        /// </summary>
        public static DMIPathFollowProvider Resolve(Object pathOrProvider, bool addIfMissing = true)
        {
            if (pathOrProvider == null)
                return null;

            if (pathOrProvider is DMIPathFollowProvider provider)
                return provider;

            if (pathOrProvider is PathCreator creator)
                return Resolve(creator, addIfMissing);

            GameObject go = pathOrProvider as GameObject;
            if (go == null && pathOrProvider is Component component)
                go = component.gameObject;

            if (go == null)
                return null;

            provider = go.GetComponent<DMIPathFollowProvider>();
            if (provider != null)
                return provider;

            creator = go.GetComponent<PathCreator>();
            return Resolve(creator, addIfMissing);
        }
    }
}
