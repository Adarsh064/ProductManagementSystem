namespace ProductManagementSystem.Services.Interface
{
    public interface IJwtService
    {
        int GetUserIdFromToken(string token);
        int GetCurrentUser();
    }
}
