using System.Collections.Generic;

namespace Postgen.Model;

internal class ApplicationDescriptor
{
    public IEnumerable<(ControllerDescriptor, IEnumerable<ControllerMethodDescriptor>)> Controllers;
}