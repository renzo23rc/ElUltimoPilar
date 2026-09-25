using System.Collections.Generic;
using UnityEngine;

namespace UltimoPilar.Core.Shared
{
    /// <summary>Destroys runtime-created materials together with the object that uses them.</summary>
    public sealed class OwnedMaterialCleanup : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials = new List<Material>();

        /// <summary>Assigns a runtime material to a renderer and releases it when the object is destroyed.</summary>
        /// <param name="renderer">The renderer that uses the material.</param>
        /// <param name="material">The runtime material owned by the renderer's object.</param>
        public static void Assign(Renderer renderer, Material material)
        {
            if (renderer == null || material == null)
            {
                return;
            }

            renderer.sharedMaterial = material;
            Track(renderer.gameObject, material);
        }

        /// <summary>Registers a runtime material to be destroyed with the supplied object.</summary>
        /// <param name="owner">The object whose destruction releases the material.</param>
        /// <param name="material">The runtime material.</param>
        public static void Track(GameObject owner, Material material)
        {
            if (owner == null || material == null)
            {
                return;
            }

            if (!owner.TryGetComponent(out OwnedMaterialCleanup cleanup))
            {
                cleanup = owner.AddComponent<OwnedMaterialCleanup>();
            }

            cleanup.ownedMaterials.Add(material);
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            ownedMaterials.Clear();
        }
    }
}
