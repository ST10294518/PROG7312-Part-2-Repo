using SmartX.API.Models;

namespace SmartX.API.Services;

public static class DeploymentValidator
{
    public static bool Validate(DeploymentNode? node)
    {
        // Base case
        if (node == null)
            return true;

        // Current node must be configured
        if (!node.IsConfigured)
            return false;

        // Recursive case
        foreach (var child in node.Children)
        {
            if (!Validate(child))
                return false;
        }

        return true;
    }
}