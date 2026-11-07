using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Features.Validation.Tests
{
    /// <summary>Static guard: ResolveByName must not scan all Resources (startup missing-script noise).</summary>
    public sealed class DMUiRuntimeAssetRegistryTests
    {
        [Test]
        public void ResolveByName_WithUnknownKey_ReturnsNullWithoutThrowing()
        {
            object hit = AssemblyCSharpTestAccess.InvokeStaticGeneric(
                "Project.UI.DMUiRuntimeAssetRegistry",
                "ResolveByName",
                typeof(PanelSettings),
                "__dm_test_nonexistent_panel_settings__");

            Assert.IsNull(hit);
        }
    }
}
