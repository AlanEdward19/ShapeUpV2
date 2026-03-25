namespace ShapeUp.Features.GymManagement;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.GymClients.AssignClientTrainer;
using ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;
using ShapeUp.Features.GymManagement.GymClients.GetGymClients;
using ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;
using ShapeUp.Features.GymManagement.GymPlans.DeleteGymPlan;
using ShapeUp.Features.GymManagement.GymPlans.GetGymPlans;
using ShapeUp.Features.GymManagement.GymPlans.UpdateGymPlan;
using ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;
using ShapeUp.Features.GymManagement.GymStaff.GetGymStaff;
using ShapeUp.Features.GymManagement.GymStaff.RemoveGymStaff;
using ShapeUp.Features.GymManagement.Gyms.CreateGym;
using ShapeUp.Features.GymManagement.Gyms.GetGyms;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Repositories;
using ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;
using ShapeUp.Features.GymManagement.PlatformTiers.DeletePlatformTier;
using ShapeUp.Features.GymManagement.PlatformTiers.GetPlatformTiers;
using ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;
using ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClients;
using ShapeUp.Features.GymManagement.TrainerClients.TransferTrainerClient;
using ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;
using ShapeUp.Features.GymManagement.TrainerPlans.DeleteTrainerPlan;
using ShapeUp.Features.GymManagement.TrainerPlans.GetTrainerPlans;
using ShapeUp.Features.GymManagement.TrainerPlans.UpdateTrainerPlan;
using ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;
using ShapeUp.Features.GymManagement.UserRoles.GetUserRoles;

public static class GymManagementModule
{
    public static IServiceCollection AddGymManagementServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<GymManagementDbContext>(options => options.UseSqlServer(connectionString));

        // Repositories
        services.AddScoped<IPlatformTierRepository, PlatformTierRepository>();
        services.AddScoped<IUserPlatformRoleRepository, UserPlatformRoleRepository>();
        services.AddScoped<IGymRepository, GymRepository>();
        services.AddScoped<IGymPlanRepository, GymPlanRepository>();
        services.AddScoped<IGymStaffRepository, GymStaffRepository>();
        services.AddScoped<IGymClientRepository, GymClientRepository>();
        services.AddScoped<ITrainerPlanRepository, TrainerPlanRepository>();
        services.AddScoped<ITrainerClientRepository, TrainerClientRepository>();

        // PlatformTiers
        services.AddScoped<CreatePlatformTierHandler>();
        services.AddScoped<IValidator<CreatePlatformTierCommand>, CreatePlatformTierValidator>();
        services.AddScoped<UpdatePlatformTierHandler>();
        services.AddScoped<IValidator<UpdatePlatformTierCommand>, UpdatePlatformTierValidator>();
        services.AddScoped<DeletePlatformTierHandler>();
        services.AddScoped<GetPlatformTiersHandler>();

        // UserRoles
        services.AddScoped<AssignUserRoleHandler>();
        services.AddScoped<IValidator<AssignUserRoleCommand>, AssignUserRoleValidator>();
        services.AddScoped<GetUserRolesHandler>();

        // Gyms
        services.AddScoped<CreateGymHandler>();
        services.AddScoped<IValidator<CreateGymCommand>, CreateGymValidator>();
        services.AddScoped<GetGymsHandler>();

        // GymPlans
        services.AddScoped<CreateGymPlanHandler>();
        services.AddScoped<IValidator<CreateGymPlanCommand>, CreateGymPlanValidator>();
        services.AddScoped<UpdateGymPlanHandler>();
        services.AddScoped<IValidator<UpdateGymPlanCommand>, UpdateGymPlanValidator>();
        services.AddScoped<DeleteGymPlanHandler>();
        services.AddScoped<GetGymPlansHandler>();

        // GymStaff
        services.AddScoped<AddGymStaffHandler>();
        services.AddScoped<IValidator<AddGymStaffCommand>, AddGymStaffValidator>();
        services.AddScoped<RemoveGymStaffHandler>();
        services.AddScoped<GetGymStaffHandler>();

        // GymClients
        services.AddScoped<EnrollGymClientHandler>();
        services.AddScoped<IValidator<EnrollGymClientCommand>, EnrollGymClientValidator>();
        services.AddScoped<AssignClientTrainerHandler>();
        services.AddScoped<GetGymClientsHandler>();

        // TrainerPlans
        services.AddScoped<CreateTrainerPlanHandler>();
        services.AddScoped<IValidator<CreateTrainerPlanCommand>, CreateTrainerPlanValidator>();
        services.AddScoped<UpdateTrainerPlanHandler>();
        services.AddScoped<IValidator<UpdateTrainerPlanCommand>, UpdateTrainerPlanValidator>();
        services.AddScoped<DeleteTrainerPlanHandler>();
        services.AddScoped<GetTrainerPlansHandler>();

        // TrainerClients
        services.AddScoped<AddTrainerClientHandler>();
        services.AddScoped<IValidator<AddTrainerClientCommand>, AddTrainerClientValidator>();
        services.AddScoped<TransferTrainerClientHandler>();
        services.AddScoped<GetTrainerClientsHandler>();

        return services;
    }
}

