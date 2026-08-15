using Project.Interaction;

namespace Project.Vendor
{
    /// <summary>
    /// Separate shop contract from quest/dialogue NPCs. Implement on vendor prefabs alongside PptNpcInteractor.
    /// </summary>
    public interface IVendor
    {
        string VendorId { get; }
        string DisplayName { get; }
        bool CanOpenShop(WorldUseContext context);
        bool TryOpenShop(WorldUseContext context);
    }
}
