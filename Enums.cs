using WebSchoolPlanner.Extensions;

namespace WebSchoolPlanner;

public static class Enums
{
    /// <summary>
    /// A Enum that provide color themes
    /// </summary>
    public enum Theme
    {
        /// <summary>
        /// The theme is chosen by the browser
        /// </summary>
        Auto,

        /// <summary>
        /// Light mode
        /// </summary>
        White,

        /// <summary>
        /// Dark mode
        /// </summary>
        Dark
    }

    /// <summary>
    /// Provides a list of supported 2fa sign in methods
    /// </summary>
    public enum TwoFactorMethods
    {
        /// <summary>
        /// Sign in via authentication app
        /// </summary>
        App = 0,

        /// <summary>
        /// Sign in via confirmation email
        /// </summary>
        Email = 1,

        /// <summary>
        /// Sign in via a recovery code
        /// </summary>
        Recovery = 2
    }
}
