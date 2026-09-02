using System;

namespace KaraokeVideoCreator.Domain.Exceptions
{
    public class InvalidProjectFileException : Exception
    {
        public InvalidProjectFileException(string message) : base(message) { }
        public InvalidProjectFileException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class UnsupportedProjectVersionException : Exception
    {
        public int ProvidedVersion { get; }
        public int SupportedVersion { get; }

        public UnsupportedProjectVersionException(int providedVersion, int supportedVersion)
            : base($"Project file version {providedVersion} is not supported. Current supported version is {supportedVersion}.")
        {
            ProvidedVersion = providedVersion;
            SupportedVersion = supportedVersion;
        }
    }
}
