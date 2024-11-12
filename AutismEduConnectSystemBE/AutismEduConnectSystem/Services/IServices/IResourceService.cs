namespace AutismEduConnectSystem.Services.IServices
{
    public interface IResourceService
    {
        string GetString(string resourceKey, params object[] parameters);
    }
}
