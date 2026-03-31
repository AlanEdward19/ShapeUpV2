namespace ShapeUp.Features.GymManagement;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using GymClients.AssignClientTrainer;
using GymClients.EnrollGymClient;
using GymClients.GetGymClients;
using GymPlans.CreateGymPlan;
using GymPlans.DeleteGymPlan;
using GymPlans.GetGymPlans;
using GymPlans.UpdateGymPlan;
using GymStaff.AddGymStaff;
using GymStaff.GetGymStaff;
using GymStaff.RemoveGymStaff;
using Gyms.CreateGym;
using Gyms.DeleteGym;
using Gyms.GetGyms;
using Gyms.UpdateGym;
using Infrastructure.Data;
using Infrastructure.Repositories;
using PlatformTiers.CreatePlatformTier;
using PlatformTiers.DeletePlatformTier;
using PlatformTiers.GetPlatformTiers;
using PlatformTiers.UpdatePlatformTier;
using Shared.Abstractions;
using TrainerClients.AcceptTrainerClientInvite;
using TrainerClients.AddTrainerClient;
using TrainerClients.GenerateTrainerClientInvite;
using TrainerClients.GetTrainerClients;
using TrainerClients.Shared;
using TrainerClients.TransferTrainerClient;
using TrainerPlans.CreateTrainerPlan;
using TrainerPlans.DeleteTrainerPlan;
using TrainerPlans.GetTrainerPlans;
using TrainerPlans.UpdateTrainerPlan;
using UserRoles.AssignUserRole;
using UserRoles.GetUserRoles;

public static class GymManagementModule
{
    public static IServiceCollection AddGymManagementServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<GymManagementDbContext>(options => options.UseSqlServer(connectionString));
        services.AddOptions<TrainerClientInviteRegisterUrlOptions>()
            .Bind(configuration.GetSection(TrainerClientInviteRegisterUrlOptions.SectionName));

        // Repositories
        services.AddScoped<IPlatformTierRepository, PlatformTierRepository>();
        services.AddScoped<IUserPlatformRoleRepository, UserPlatformRoleRepository>();
        services.AddScoped<IGymRepository, GymRepository>();
        services.AddScoped<IGymPlanRepository, GymPlanRepository>();
        services.AddScoped<IGymStaffRepository, GymStaffRepository>();
        services.AddScoped<IGymClientRepository, GymClientRepository>();
        services.AddScoped<ITrainerPlanRepository, TrainerPlanRepository>();
        services.AddScoped<ITrainerClientRepository, TrainerClientRepository>();
        services.AddScoped<ITrainerClientInviteRepository, TrainerClientInviteRepository>();
        services.AddScoped<ITrainerClientInvitePayloadCodec, TrainerClientInvitePayloadCodec>();
        services.AddScoped<ITrainerClientInviteRegisterUrlBuilder, TrainerClientInviteRegisterUrlBuilder>();

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
        services.AddScoped<UpdateGymHandler>();
        services.AddScoped<IValidator<UpdateGymCommand>, UpdateGymValidator>();
        services.AddScoped<DeleteGymHandler>();
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
        services.AddScoped<GenerateTrainerClientInviteHandler>();
        services.AddScoped<IValidator<GenerateTrainerClientInviteCommand>, GenerateTrainerClientInviteValidator>();
        services.AddScoped<AcceptTrainerClientInviteHandler>();
        services.AddScoped<IValidator<AcceptTrainerClientInviteCommand>, AcceptTrainerClientInviteValidator>();
        services.AddScoped<TransferTrainerClientHandler>();
        services.AddScoped<GetTrainerClientsHandler>();

        return services;
    }
}

