using Microsoft.AspNetCore.Identity;

namespace RealEstateAdmin.Data;

public static class SeedData
{
    public static async Task Initialize(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Créer les rôles
        string[] roleNames = { "Admin", "Agent", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Créer un utilisateur Admin par défaut
        var adminEmail = "admin@realestate.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrateur",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Créer un Agent par défaut
        var agentEmail = "agent@realestate.com";
        var agentUser = await userManager.FindByEmailAsync(agentEmail);
        
        if (agentUser == null)
        {
            agentUser = new ApplicationUser
            {
                UserName = agentEmail,
                Email = agentEmail,
                FullName = "Agent Immobilier",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(agentUser, "Agent123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(agentUser, "Agent");
            }
        }

        // Créer un Utilisateur standard par défaut
        var userEmail = "user@realestate.com";
        var standardUser = await userManager.FindByEmailAsync(userEmail);
        
        if (standardUser == null)
        {
            standardUser = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                FullName = "Utilisateur Standard",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(standardUser, "User123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(standardUser, "User");
            }
        }
    }
}