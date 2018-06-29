
namespace M8.Animator {
    [System.Flags]
    public enum AxisFlags {
        None = 0,
        All = ~0,

        X = 1,
        Y = 2,
        Z = 4,
    }
}