namespace LatticeBoltzmann.Interfaces
{
    public interface ISetting<T> : ISetting
    {
        T Value { get; set; }
    }
}
