namespace Project.Environment.Doors
{
    public enum DMSlidingDoorMotionMode
    {
        Slide = 0,
        Rotate = 1
    }

    /// <summary>Local-space axis on each door leaf (frame rotation defines world direction).</summary>
    public enum DMSlidingDoorLocalAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    /// <summary>How the two leaves move relative to each other.</summary>
    public enum DMSlidingDoorOpenDirection
    {
        /// <summary>Left leaf uses negative axis sign, right leaf positive (typical double sliding door).</summary>
        PanelSymmetric = 0,
        /// <summary>Both leaves move along the same local axis direction.</summary>
        SameDirection = 1,
        /// <summary>Use <see cref="DMSlidingDoorProfile.leftPanelSign"/> / <see cref="DMSlidingDoorProfile.rightPanelSign"/>.</summary>
        Custom = 2
    }
}
