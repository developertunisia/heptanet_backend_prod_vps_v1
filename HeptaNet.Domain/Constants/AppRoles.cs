namespace HeptaNet.Domain.Constants;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Utilisateur = "Utilisateur";
    
    public static readonly string[] AllRoles = 
    {
        SuperAdmin,
        Admin,
        Utilisateur
    };
}
