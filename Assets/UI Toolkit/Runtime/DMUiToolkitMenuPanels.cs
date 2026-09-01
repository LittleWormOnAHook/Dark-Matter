namespace Project.UI
{
    /// <summary>Facade for menu sub-panels and open-state checks (input routing).</summary>
    public static class DMUiToolkitMenuPanels
    {
        public static bool IsAnySubPanelOpen =>
            DMUiToolkitSettings.IsOpen
            || DMUiToolkitControls.IsOpen
            || DMUiToolkitSaveSlots.IsOpen;

        public static bool TryHandleEscapeBack()
        {
            if (DMUiToolkitSettings.HandleBack())
                return true;
            if (DMUiToolkitControls.HandleBack())
                return true;
            if (DMUiToolkitSaveSlots.HandleBack())
                return true;
            return false;
        }

        public static void EnsureHosts()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return;

            DMUiToolkitSettings.EnsureHost();
            DMUiToolkitControls.EnsureHost();
            DMUiToolkitSaveSlots.EnsureHost();
        }
    }
}
