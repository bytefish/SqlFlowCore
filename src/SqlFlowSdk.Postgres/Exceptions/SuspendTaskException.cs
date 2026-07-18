// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SqlServerFlowSdk.Exceptions;

public class SuspendTaskException : Exception
{
    public SuspendTaskException() : base("Task suspended") { }
}
