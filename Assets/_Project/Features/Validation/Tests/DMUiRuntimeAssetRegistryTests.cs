using NUnit.Framework;
using Project.UI;
using UnityEngine;

namespace Project.Validation.Tests
{
    /// <summary>Static guard: ResolveByName must not scan all Resources (startup missing-script noise).</summary>
    public sealed class DMUiRuntimeAssetRegistryTests
    {
        [Test]
        public void ResolveByName_WithUnknownKey_ReturnsNullWithoutThrowing()
        {
            PanelSettings hit = DMUiRuntimeAssetRegistry.ResolveByName<PanelSettings>("__dm_test_nonexistent_panel_settings__");
            Assert.IsNull(hit);
        }
    }
}
