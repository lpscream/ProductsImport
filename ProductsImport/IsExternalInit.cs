namespace System.Runtime.CompilerServices;

// .NET Framework doesn't ship this marker type, which the C# compiler requires to allow
// `init` accessors and positional records. Modern SDKs normally embed it automatically for
// down-level targets, but that doesn't always happen (older SDK/MSBuild versions), so it's
// declared here explicitly.
internal static class IsExternalInit
{
}
