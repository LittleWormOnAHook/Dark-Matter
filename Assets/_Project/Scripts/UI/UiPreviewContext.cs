namespace Project.UI
{
    /// <summary>
    /// Editor sandbox / preview flags — suppresses runtime bootstraps while UI Studio builds preview trees.
    /// </summary>
    public static class UiPreviewContext
    {
        public static bool IsActive { get; set; }
        public static bool IsSandbox { get; set; }
    }
}
