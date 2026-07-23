using System;

namespace InstagramClone.API.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuthorizeOwnershipAttribute : Attribute
    {
        public Type ResourceType { get; }
        public string RouteIdParameterName { get; }

        public AuthorizeOwnershipAttribute(Type resourceType, string routeIdParameterName = "id")
        {
            ResourceType = resourceType;
            RouteIdParameterName = routeIdParameterName;
        }
    }
}
