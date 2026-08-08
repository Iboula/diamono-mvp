using Diamono.Application.Abstractions;
using Diamono.Application.Administration;
using Diamono.Application.Audit;
using Diamono.Application.Security;
using Diamono.Domain.Audit;
using Diamono.Domain.Security;
using Diamono.Infrastructure.Identity;
using Diamono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class UserAdministrationServiceTests
{
    private const string ValidPassword = "Aa!123456789";

    [Fact]
    public async Task Utilisateur_sans_Administration_Manage_interdit()
    {
        await using var fixture = await UserAdminFixture.CreateAsync(hasAdministrationManage: false);

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(() => fixture.Service.GetUsersAsync());

        Assert.Equal(Permissions.AdministrationManage, error.Permission);
    }

    [Fact]
    public async Task SuperAdmin_peut_lister_les_utilisateurs()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();

        var users = await fixture.Service.GetUsersAsync();

        Assert.Contains(users, user => user.Email == fixture.SuperAdmin.Email);
    }

    [Fact]
    public async Task Creation_Gestionnaire_reussit()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();

        var result = await fixture.Service.CreateUserAsync(new CreateUserRequest(
            "Gestionnaire Test",
            "gestionnaire@diamono.local",
            DiamonoRoles.Gestionnaire));

        var user = await fixture.UserManager.FindByEmailAsync("gestionnaire@diamono.local");
        Assert.NotNull(user);
        Assert.Equal(DiamonoRoles.Gestionnaire, result.User.Role);
        Assert.True(await fixture.UserManager.IsInRoleAsync(user, DiamonoRoles.Gestionnaire));
        Assert.True(await fixture.UserManager.CheckPasswordAsync(user, result.TemporaryPassword));
        Assert.Contains(fixture.Db.AuditLogs, entry => entry.Action == AuditActions.UserCreated && entry.EntityId == user.Id.ToString());
    }

    [Fact]
    public async Task Creation_Caissier_reussit()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();

        var result = await fixture.Service.CreateUserAsync(new CreateUserRequest(
            "Caissier Test",
            "caissier@diamono.local",
            DiamonoRoles.Caissier));

        Assert.Equal(DiamonoRoles.Caissier, result.User.Role);
    }

    [Fact]
    public async Task Email_duplique_refuse()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();
        await fixture.CreateUserAsync("duplicate@diamono.local", DiamonoRoles.Gestionnaire);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateUserAsync(
            new CreateUserRequest("Duplicate", "duplicate@diamono.local", DiamonoRoles.Lecteur)));
    }

    [Fact]
    public async Task Role_inconnu_refuse()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateUserAsync(
            new CreateUserRequest("Role Inconnu", "role@diamono.local", "Coach")));
    }

    [Fact]
    public async Task Changement_Gestionnaire_vers_Caissier_met_a_jour_role_et_stamp()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("role-change@diamono.local", DiamonoRoles.Gestionnaire);
        var initialStamp = user.SecurityStamp;

        await fixture.Service.UpdateUserRoleAsync(user.Id.ToString(), DiamonoRoles.Caissier);

        user = (await fixture.UserManager.FindByIdAsync(user.Id.ToString()))!;
        var roles = await fixture.UserManager.GetRolesAsync(user);
        Assert.DoesNotContain(DiamonoRoles.Gestionnaire, roles);
        Assert.Contains(DiamonoRoles.Caissier, roles);
        Assert.NotEqual(initialStamp, user.SecurityStamp);
        var audit = Assert.Single(fixture.Db.AuditLogs.Where(entry => entry.Action == AuditActions.UserRoleChanged));
        Assert.Contains("Gestionnaire", audit.OldValuesJson);
        Assert.Contains("Caissier", audit.NewValuesJson);
    }

    [Fact]
    public async Task Desactivation_empeche_connexion()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("disabled@diamono.local", DiamonoRoles.Gestionnaire);

        await fixture.Service.DisableUserAsync(user.Id.ToString());

        user = (await fixture.UserManager.FindByIdAsync(user.Id.ToString()))!;
        var result = await fixture.SignInManager.CheckPasswordSignInAsync(user, ValidPassword, lockoutOnFailure: false);
        Assert.True(result.IsLockedOut);
        Assert.Contains(fixture.Db.AuditLogs, entry => entry.Action == AuditActions.UserDisabled && entry.EntityId == user.Id.ToString());
    }

    [Fact]
    public async Task Reactivation_permet_de_nouveau_connexion()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("enabled@diamono.local", DiamonoRoles.Gestionnaire);

        await fixture.Service.DisableUserAsync(user.Id.ToString());
        await fixture.Service.EnableUserAsync(user.Id.ToString());

        user = (await fixture.UserManager.FindByIdAsync(user.Id.ToString()))!;
        var result = await fixture.SignInManager.CheckPasswordSignInAsync(user, ValidPassword, lockoutOnFailure: false);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SuperAdmin_ne_peut_pas_desactiver_son_propre_compte()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.DisableUserAsync(fixture.SuperAdmin.Id.ToString()));
    }

    [Fact]
    public async Task Dernier_SuperAdmin_ne_peut_pas_perdre_son_role()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();
        fixture.CurrentUserId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateUserRoleAsync(fixture.SuperAdmin.Id.ToString(), DiamonoRoles.Lecteur));
    }

    [Fact]
    public async Task SecurityStamp_est_mis_a_jour_lors_d_une_desactivation()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();
        var user = await fixture.CreateUserAsync("stamp@diamono.local", DiamonoRoles.Gestionnaire);
        var initialStamp = user.SecurityStamp;

        await fixture.Service.DisableUserAsync(user.Id.ToString());

        user = (await fixture.UserManager.FindByIdAsync(user.Id.ToString()))!;
        Assert.NotEqual(initialStamp, user.SecurityStamp);
    }

    [Fact]
    public async Task Audit_utilisateur_ne_serialise_pas_de_secret()
    {
        await using var fixture = await UserAdminFixture.CreateAsync();

        var result = await fixture.Service.CreateUserAsync(new CreateUserRequest(
            "Secret Check",
            "secret-check@diamono.local",
            DiamonoRoles.Lecteur));

        var serializedAudit = string.Join(" ", fixture.Db.AuditLogs.Select(x =>
            $"{x.OldValuesJson} {x.NewValuesJson} {x.MetadataJson}"));

        Assert.DoesNotContain(result.TemporaryPassword, serializedAudit);
        Assert.DoesNotContain("PasswordHash", serializedAudit);
        Assert.DoesNotContain("SecurityStamp", serializedAudit);
    }

    private sealed class UserAdminFixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        private readonly TestOperatorContext operatorContext;

        private UserAdminFixture(ServiceProvider provider, TestOperatorContext operatorContext, ApplicationUser superAdmin)
        {
            this.provider = provider;
            this.operatorContext = operatorContext;
            SuperAdmin = superAdmin;
        }

        public IUserAdministrationService Service => provider.GetRequiredService<IUserAdministrationService>();
        public DiamonoDbContext Db => provider.GetRequiredService<DiamonoDbContext>();
        public UserManager<ApplicationUser> UserManager => provider.GetRequiredService<UserManager<ApplicationUser>>();
        public SignInManager<ApplicationUser> SignInManager => provider.GetRequiredService<SignInManager<ApplicationUser>>();
        public ApplicationUser SuperAdmin { get; }

        public string CurrentUserId
        {
            get => operatorContext.CurrentUserId;
            set => operatorContext.CurrentUserId = value;
        }

        public static async Task<UserAdminFixture> CreateAsync(bool hasAdministrationManage = true)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<DiamonoDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddHttpContextAccessor();
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<DiamonoDbContext>()
            .AddDefaultTokenProviders();

            var context = new TestOperatorContext(hasAdministrationManage);
            services.AddSingleton<IPermissionGuard>(context);
            services.AddSingleton<ICurrentUserAccessor>(context);
            services.AddScoped<IAuditWriter, AuditWriter>();
            services.AddScoped<IUserAdministrationService, UserAdministrationService>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
            {
                RequestServices = provider
            };

            var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
            foreach (var role in DiamonoRoles.All)
                await roleManager.CreateAsync(new ApplicationRole(role));

            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var superAdmin = new ApplicationUser
            {
                UserName = "admin@diamono.local",
                Email = "admin@diamono.local",
                EmailConfirmed = true,
                DisplayName = "Admin",
                LockoutEnabled = true
            };
            await userManager.CreateAsync(superAdmin, ValidPassword);
            await userManager.AddToRoleAsync(superAdmin, DiamonoRoles.SuperAdmin);
            context.CurrentUserId = superAdmin.Id.ToString();

            return new UserAdminFixture(provider, context, superAdmin);
        }

        public async Task<ApplicationUser> CreateUserAsync(string email, string role)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = email,
                LockoutEnabled = true
            };
            await UserManager.CreateAsync(user, ValidPassword);
            await UserManager.AddToRoleAsync(user, role);
            return user;
        }

        public async ValueTask DisposeAsync()
            => await provider.DisposeAsync();
    }

    private sealed class TestOperatorContext(bool hasAdministrationManage) : IPermissionGuard, ICurrentUserAccessor
    {
        public string CurrentUserId { get; set; } = string.Empty;

        public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
            => Task.FromResult(hasAdministrationManage && permission == Permissions.AdministrationManage);

        public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
            => hasAdministrationManage && permission == Permissions.AdministrationManage
                ? Task.CompletedTask
                : throw new PermissionDeniedException(permission);

        public Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<CurrentUser?>(new CurrentUser(
                CurrentUserId,
                "admin@diamono.local",
                "Admin",
                [DiamonoRoles.SuperAdmin],
                [Permissions.AdministrationManage]));
    }
}
