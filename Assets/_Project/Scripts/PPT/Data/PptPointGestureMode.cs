namespace Project.PPT
{
    public enum PptPointGestureMode
    {
        /// <summary>Rotate visual root toward bearing, play point on base layer.</summary>
        FullBody = 0,

        /// <summary>Keep lower body / seated pose on base layer; point on masked upper-body layer.
        /// Visual root can still yaw toward the bearing when RotateVisualTowardBearing is enabled.</summary>
        UpperBodyOnly = 1
    }
}
