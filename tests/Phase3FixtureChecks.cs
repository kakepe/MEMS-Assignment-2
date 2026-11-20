using System.Security.Cryptography;
using System.Text.Json;
using MEMS.Api.Infrastructure;
using MEMS.Data;
using MEMS.Data.Identity;
using MEMS.Data.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MEMS.Api.Verification;

internal static class Phase3FixtureChecks
{
    private sealed record Fixture(string UserId,string UserName,string Password,string BeforeFingerprint);
    public static async Task RunAsync(IServiceProvider services,IConfiguration configuration,bool development,bool prepare)
    {
        if(!development)throw new InvalidOperationException("Verification fixture commands are Development-only.");
        await using var scope=services.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<MemsDbContext>();DatabaseInitialization.RequireAssignment2(db);
        var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var local=configuration["Mems:LocalStorage"]??throw new InvalidOperationException("Local verification storage is required.");
        var path=Path.Combine(local,"phase3-fixtures.json");var connection=configuration.GetConnectionString("mems")!;
        if(prepare)
        {
            if(File.Exists(path))throw new InvalidOperationException("Existing verification fixtures must be completed, not overwritten.");
            var before=await Phase2DatabaseChecks.Fingerprint(connection);
            var tenant=await db.Municipalities.IgnoreQueryFilters().SingleAsync(x=>x.Slug=="burnaby");
            var name="phase3.lockout."+Guid.NewGuid().ToString("N");var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))+"aA1!";
            var user=new ApplicationUser{UserName=name,Scope=AdministratorScope.Municipality,MunicipalityId=tenant.MunicipalityId};
            Check(await users.CreateAsync(user,password));Check(await users.AddToRoleAsync(user,AdministratorRoles.MunicipalityAdmin));
            await File.WriteAllTextAsync(path,JsonSerializer.Serialize(new Fixture(user.Id,name,password,before)));
            Console.WriteLine("Prepared a normal verification-owned Identity account for lockout testing; private credentials retained locally.");
        }
        else
        {
            var state=JsonSerializer.Deserialize<Fixture>(await File.ReadAllTextAsync(path))!;
            var user=await users.FindByIdAsync(state.UserId);
            if(user is not null)
            {
                if(user.UserName!=state.UserName||!user.UserName.StartsWith("phase3.lockout.",StringComparison.Ordinal))throw new InvalidOperationException("Refusing to remove an unowned account.");
                Check(await users.RemoveFromRolesAsync(user,await users.GetRolesAsync(user)));Check(await users.DeleteAsync(user));
            }
            if(await Phase2DatabaseChecks.Fingerprint(connection)!=state.BeforeFingerprint)
                throw new InvalidOperationException("Database records differ from the Phase 3 baseline; finish owned-record cleanup before completion.");
            // Pure session-lifetime checks, not a test HTTP authentication scheme or endpoint.
            var sessions=scope.ServiceProvider.GetRequiredService<IdentitySessions>();
            System.Security.Claims.ClaimsPrincipal Session(long expiry)=>new(new System.Security.Claims.ClaimsIdentity([
                new(IdentitySessions.ClaimType,Guid.NewGuid().ToString("N")),new(IdentitySessions.ExpiresClaimType,expiry.ToString())]));
            if(sessions.IsActive(Session(DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds())) ||
                !sessions.IsActive(Session(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds())))
                throw new InvalidOperationException("Connected session expiration verification failed.");
            var context=new DefaultHttpContext();context.RequestServices=services;context.Request.IsHttps=true;context.Response.Body=new MemoryStream();
            await new ApiProblems(_=>throw new InvalidOperationException("PRIVATE_TEST_VALUE_DO_NOT_EXPOSE"),NullLogger<ApiProblems>.Instance).InvokeAsync(context);
            context.Response.Body.Position=0;var text=await new StreamReader(context.Response.Body).ReadToEndAsync();
            if(context.Response.StatusCode!=500||text.Contains("PRIVATE_TEST_VALUE")||text.Contains("InvalidOperationException"))throw new InvalidOperationException("Safe 500 response verification failed.");
            var root=Directory.GetParent(local)!.FullName;Directory.CreateDirectory(Path.Combine(root,"artifacts/phase3"));
            await File.WriteAllTextAsync(Path.Combine(root,"artifacts/phase3/database-preservation.json"),JsonSerializer.Serialize(new{passed=true,existingRecordsAndPasswordHashesUnchanged=true,safeGeneric500Verified=true,connectedSessionExpirationVerified=true}));
            File.Move(path,Path.Combine(local,"phase3-fixtures-completed-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".json"));
            Console.WriteLine("PASS: complete pre-test database record/password fingerprint restored; owned account removed; generic 500 response leaks no internal details.");
        }
    }
    private static void Check(IdentityResult result){if(!result.Succeeded)throw new InvalidOperationException("Fixture Identity operation failed: "+string.Join(',',result.Errors.Select(x=>x.Code)));}
}
