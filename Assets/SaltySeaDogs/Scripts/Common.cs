namespace SaltySeaDogs
{
    /// <summary>
    /// Status of sails during and after transitions
    /// </summary>
    public enum SailStatus
    {
        Furled,
        Set,
        Furling,
        Setting
    }

    /// <summary>
    /// Sail setting for the entire ship
    /// </summary>
    public enum SailSetting
    {
        Furled,
        Battle,
        Full
    }

    /// <summary>
    /// Type of sail. This determines wind effects.
    /// </summary>
    public enum SailType
    {
        Staysail,
        Squaresail,
        Gaff
    }

    /// <summary>
    /// Used to determine which direction the wind is coming from. Based on yard position.
    /// </summary>
    public enum ShipSide
    {
        Port,
        Stbd,
        Bow,
        Stern
    }

    public enum RotationalAxes
    {
        X,
        Y,
        Z
    }

    public enum GunStatus
    {
        Reloading,
        Ready
    }
}
