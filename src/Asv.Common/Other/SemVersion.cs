using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    /// <summary>
    /// A semantic version implementation.
    /// Conforms to v2.0.0 of http://semver.org/
    /// </summary>
    [Serializable]
    public sealed class SemVersion : IComparable<SemVersion>, IComparable, ISerializable
    {
        static Regex parseEx =
            new(@"^(?<major>\d+)" +
                @"(\.(?<minor>\d+))?" +
                @"(\.(?<patch>\d+))?" +
                @"(\-(?<pre>[0-9A-Za-z\-\.]+))?" +
                @"(\+(?<build>[0-9A-Za-z\-\.]+))?$",
                RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Initializes a new instance of the <see cref="SemVersion" /> class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private SemVersion(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            var semVersion = Parse(info.GetString("SemVersion"));
            Major = semVersion.Major;
            Minor = semVersion.Minor;
            Patch = semVersion.Patch;
            Prerelease = semVersion.Prerelease;
            Build = semVersion.Build;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemVersion" /> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="prerelease">The prerelease version (eg. "alpha").</param>
        /// <param name="build">The build eg ("nightly.232").</param>
        public SemVersion(int major, int minor = 0, int patch = 0, string? prerelease = null, string? build = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            // strings are interned to be able to compare by reference in equals method
            Prerelease = string.Intern(prerelease ?? string.Empty);
            Build = string.Intern(build ?? string.Empty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemVersion"/> class.
        /// </summary>
        /// <param name="version">The <see cref="System.Version"/> that is used to initialize 
        /// the Major, Minor, Patch and Build properties.</param>
        public SemVersion(Version version)
        {
            version = version ?? new Version();

            Major = version.Major;
            Minor = version.Minor;

            if (version.Revision >= 0)
            {
                Patch = version.Revision;
            }

            Prerelease = String.Intern(string.Empty);

            if (version.Build > 0)
            {
                Build = String.Intern(version.Build.ToString());
            }
            else
            {
                Build = String.Intern(string.Empty);
            }
        }

        /// <summary>
        /// Parses the specified string to a semantic version.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="strict">If set to <c>true</c> minor and patch version are required, else they default to 0.</param>
        /// <returns>The SemVersion object.</returns>
        /// <exception cref="System.InvalidOperationException">When a invalid version string is passed.</exception>
        public static SemVersion Parse(string? version, bool strict = false)
        {
            var match = parseEx.Match(version ?? throw new ArgumentNullException(nameof(version)));
            if (!match.Success)
                throw new ArgumentException("Invalid version.", nameof(version));

            var major = Int32.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);

            var minorMatch = match.Groups["minor"];
            var minor = 0;
            if (minorMatch.Success)
                minor = Int32.Parse(minorMatch.Value, CultureInfo.InvariantCulture);
            else if (strict)
                throw new InvalidOperationException("Invalid version (no minor version given in strict mode)");

            var patchMatch = match.Groups["patch"];
            var patch = 0;
            if (patchMatch.Success)
                patch = Int32.Parse(patchMatch.Value, CultureInfo.InvariantCulture);
            else if (strict)
                throw new InvalidOperationException("Invalid version (no patch version given in strict mode)");

            var prerelease = match.Groups["pre"].Value;
            var build = match.Groups["build"].Value;

            return new SemVersion(major, minor, patch, prerelease, build);
        }

        /// <summary>
        /// Parses the specified string to a semantic version.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="semver">When the method returns, contains a SemVersion instance equivalent 
        /// to the version string passed in, if the version string was valid, or <c>null</c> if the 
        /// version string was not valid.</param>
        /// <param name="strict">If set to <c>true</c> minor and patch version are required, else they default to 0.</param>
        /// <returns><c>False</c> when a invalid version string is passed, otherwise <c>true</c>.</returns>
        public static bool TryParse(string version, out SemVersion? semver, bool strict = false)
        {
            try
            {
                semver = Parse(version, strict);
                return true;
            }
            catch (Exception)
            {
                semver = null;
                return false;
            }
        }

        /// <summary>
        /// Tests the specified versions for equality.
        /// </summary>
        /// <param name="versionA">The first version.</param>
        /// <param name="versionB">The second version.</param>
        /// <returns>If versionA is equal to versionB <c>true</c>, else <c>false</c>.</returns>
        public static bool Equals(SemVersion versionA, SemVersion versionB)
        {
            if (ReferenceEquals(versionA, null))
                return ReferenceEquals(versionB, null);
            return versionA.Equals(versionB);
        }

        /// <summary>
        /// Compares the specified versions.
        /// </summary>
        /// <param name="versionA">The version to compare to.</param>
        /// <param name="versionB">The version to compare against.</param>
        /// <returns>If versionA &lt; versionB <c>&lt; 0</c>, if versionA &gt; versionB <c>&gt; 0</c>,
        /// if versionA is equal to versionB <c>0</c>.</returns>
        public static int Compare(SemVersion versionA, SemVersion versionB)
        {
            if (ReferenceEquals(versionA, null))
                return ReferenceEquals(versionB, null) ? 0 : -1;
            return versionA.CompareTo(versionB);
        }

        /// <summary>
        /// Make a copy of the current instance with optional altered fields. 
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="prerelease">The prerelease text.</param>
        /// <param name="build">The build text.</param>
        /// <returns>The new version object.</returns>
        public SemVersion Change(int? major = null, int? minor = null, int? patch = null,
            string? prerelease = null, string? build = null)
        {
            return new SemVersion(
                major ?? Major,
                minor ?? Minor,
                patch ?? Patch,
                prerelease ?? Prerelease,
                build ?? Build);
        }

        /// <summary>
        /// Gets the major version.
        /// </summary>
        /// <value>
        /// The major version.
        /// </value>
        public int Major { get; private set; }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        /// <value>
        /// The minor version.
        /// </value>
        public int Minor { get; private set; }

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        /// <value>
        /// The patch version.
        /// </value>
        public int Patch { get; private set; }

        /// <summary>
        /// Gets the pre-release version.
        /// </summary>
        /// <value>
        /// The pre-release version.
        /// </value>
        public string Prerelease { get; private set; }

        /// <summary>
        /// Gets the build version.
        /// </summary>
        /// <value>
        /// The build version.
        /// </value>
        public string Build { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var version = string.Empty + Major + "." + Minor + "." + Patch;
            if (!String.IsNullOrEmpty(Prerelease))
                version += "-" + Prerelease;
            if (!String.IsNullOrEmpty(Build))
                version += "+" + Build;
            return version;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates 
        /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the 
        /// other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. 
        /// The return value has these meanings: Value Meaning Less than zero 
        ///  This instance precedes <paramref name="obj" /> in the sort order. 
        ///  Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. i
        ///  Greater than zero This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        public int CompareTo(object? obj)
        {
            return CompareTo(obj as SemVersion);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates 
        /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the 
        /// other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. 
        /// The return value has these meanings: Value Meaning Less than zero 
        ///  This instance precedes <paramref name="other" /> in the sort order. 
        ///  Zero This instance occurs in the same position in the sort order as <paramref name="other" />. i
        ///  Greater than zero This instance follows <paramref name="other" /> in the sort order.
        /// </returns>
        public int CompareTo(SemVersion? other)
        {
            if (ReferenceEquals(other, null))
                return 1;

            var r = CompareByPrecedence(other);
            if (r != 0)
                return r;

            r = CompareComponent(Build, other.Build);
            return r;
        }

        /// <summary>
        /// Compares to semantic versions by precedence. This does the same as a Equals, but ignores the build information.
        /// </summary>
        /// <param name="other">The semantic version.</param>
        /// <returns><c>true</c> if the version precedence matches.</returns>
        public bool PrecedenceMatches(SemVersion other)
        {
            return CompareByPrecedence(other) == 0;
        }

        /// <summary>
        /// Compares to semantic versions by precedence. This does the same as a Equals, but ignores the build information.
        /// </summary>
        /// <param name="other">The semantic version.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. 
        /// The return value has these meanings: Value Meaning Less than zero 
        ///  This instance precedes <paramref name="other" /> in the version precedence.
        ///  Zero This instance has the same precedence as <paramref name="other" />. i
        ///  Greater than zero This instance has creater precedence as <paramref name="other" />.
        /// </returns>
        public int CompareByPrecedence(SemVersion other)
        {
            if (ReferenceEquals(other, null))
                return 1;

            var r = Major.CompareTo(other.Major);
            if (r != 0) return r;

            r = Minor.CompareTo(other.Minor);
            if (r != 0) return r;

            r = Patch.CompareTo(other.Patch);
            if (r != 0) return r;

            r = CompareComponent(Prerelease, other.Prerelease, true);
            return r;
        }

        static int CompareComponent(string a, string b, bool lower = false)
        {
            var aEmpty = String.IsNullOrEmpty(a);
            var bEmpty = String.IsNullOrEmpty(b);
            if (aEmpty && bEmpty)
                return 0;

            if (aEmpty)
                return lower ? 1 : -1;
            if (bEmpty)
                return lower ? -1 : 1;

            var aComps = a.Split('.');
            var bComps = b.Split('.');

            var minLen = Math.Min(aComps.Length, bComps.Length);
            for (var i = 0; i < minLen; i++)
            {
                var ac = aComps[i];
                var bc = bComps[i];
                int anum, bnum;
                var isanum = Int32.TryParse(ac, out anum);
                var isbnum = Int32.TryParse(bc, out bnum);
                int r;
                if (isanum && isbnum)
                {
                    r = anum.CompareTo(bnum);
                    if (r != 0) return anum.CompareTo(bnum);
                }
                else
                {
                    if (isanum)
                        return -1;
                    if (isbnum)
                        return 1;
                    r = String.CompareOrdinal(ac, bc);
                    if (r != 0)
                        return r;
                }
            }

            return aComps.Length.CompareTo(bComps.Length);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            var other = (SemVersion)obj;

            // do string comparison by reference (possible because strings are interned in ctor)
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   ReferenceEquals(Prerelease, other.Prerelease) &&
                   ReferenceEquals(Build, other.Build);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var result = Major.GetHashCode();
                result = result * 31 + Minor.GetHashCode();
                result = result * 31 + Patch.GetHashCode();
                result = result * 31 + Prerelease.GetHashCode();
                result = result * 31 + Build.GetHashCode();
                return result;
            }
        }

        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");
            info.AddValue("SemVersion", ToString());
        }

        /// <summary>
        /// Implicit conversion from string to SemVersion.
        /// </summary>
        /// <param name="version">The semantic version.</param>
        /// <returns>The SemVersion object.</returns>
        public static implicit operator SemVersion(string version)
        {
            return Parse(version);
        }

        /// <summary>
        /// The override of the equals operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator ==(SemVersion left, SemVersion right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// The override of the un-equal operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is not equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator !=(SemVersion left, SemVersion right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// The override of the greater operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is greater than right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator >(SemVersion left, SemVersion right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// The override of the greater than or equal operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is greater than or equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator >=(SemVersion left, SemVersion right)
        {
            return left == right || left > right;
        }

        /// <summary>
        /// The override of the less operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is less than right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator <(SemVersion left, SemVersion right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// The override of the less than or equal operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is less than or equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator <=(SemVersion left, SemVersion right)
        {
            return left == right || left < right;
        }
    }
}
