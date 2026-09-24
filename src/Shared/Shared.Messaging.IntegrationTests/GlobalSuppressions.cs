// CA1716 fires on every namespace under the Shared.* root because "Shared" is a VB.NET keyword.
// The root is mandated by the repository layout (docs/rules/backend-module-structure.md) and is
// consumed from C# only, so each namespace is suppressed individually here rather than renamed.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Shared.* root namespace is mandated by the repository layout; 'Shared' is only a keyword in VB.NET.",
    Scope = "namespace",
    Target = "~N:Shared.Messaging.IntegrationTests.Dapper")]
[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Shared.* root namespace is mandated by the repository layout; 'Shared' is only a keyword in VB.NET.",
    Scope = "namespace",
    Target = "~N:Shared.Messaging.IntegrationTests.Ef")]
[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Shared.* root namespace is mandated by the repository layout; 'Shared' is only a keyword in VB.NET.",
    Scope = "namespace",
    Target = "~N:Shared.Messaging.IntegrationTests.Fixtures")]
[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Shared.* root namespace is mandated by the repository layout; 'Shared' is only a keyword in VB.NET.",
    Scope = "namespace",
    Target = "~N:Shared.Messaging.IntegrationTests.Roundtrip")]
