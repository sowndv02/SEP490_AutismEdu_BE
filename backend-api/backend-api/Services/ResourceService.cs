using backend_api.Services.IServices;

namespace backend_api.Services
{
    public class ResourceService : IResourceService
    {
        public string GetString(string resourceKey, params object[] parameters)
        {
            var message = Resources.Messages.ResourceManager.GetString(resourceKey);

            return !string.IsNullOrEmpty(message) && parameters?.Length > 0
                ? string.Format(message, parameters)
                : message;
        }
    }
}
