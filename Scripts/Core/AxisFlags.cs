
namespace M8.Animator {
    [System.Flags]
    public enum AxisFlags {
        None = 0,
        All = X | Y | Z,

        X = 1,
        Y = 2,
        Z = 4,
    }
}