namespace Satori.Utilities
{
    public class CssClass(string className)
    {
        private string ClassName { get; } = className;

        public override string ToString() => ClassName;

        public static CssClass None { get; } = new(string.Empty);

        #region Equality

        private bool Equals(CssClass other)
        {
            return ClassName == other.ClassName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return string.IsNullOrWhiteSpace(ClassName);
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((CssClass)obj);
        }

        public override int GetHashCode()
        {
            return ClassName.GetHashCode();
        }

        public static bool operator ==(CssClass? left, CssClass? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CssClass? left, CssClass? right)
        {
            return !Equals(left, right);
        }

        #endregion Equality
    }
}
