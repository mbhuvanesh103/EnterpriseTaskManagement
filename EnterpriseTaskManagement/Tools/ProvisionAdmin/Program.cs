using System.Text;

using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Authentication;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using EnterpriseTaskManagement.Application;
using EnterpriseTaskManagement.Infrastructure;

namespace EnterpriseTaskManagement.Tools.ProvisionAdmin;

/// <summary>
/// Temporary, local-only console tool to provision the very first Admin user
/// into an empty TaskFlowDb database. It reuses the existing domain model,
/// repositories, unit of work, and IPasswordHasher implementation exactly as
/// the production API does. It is not part of the production application and
/// must be deleted after successful provisioning.
///
/// No plaintext password is ever stored, logged, or echoed. Secrets (connection
/// string, JWT key) are read exclusively from environment variables.
/// </summary>
public static class Program
{
    private const string DefaultDepartmentName = "Administration";
    private const string DefaultDepartmentCode = "ADMIN";

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var configuration = BuildConfiguration();
            var services = new ServiceCollection();

            services.AddApplication();
            services.AddInfrastructure(configuration);

            using var serviceProvider = services.BuildServiceProvider();

            var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
            var departmentRepository = serviceProvider.GetRequiredService<IDepartmentRepository>();
            var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
            var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

            // Safety: refuse to run if any user already exists. Never overwrite or delete existing data.
            var existingUsers = await userRepository.ListAllAsync();
            if (existingUsers.Count > 0)
            {
                Console.Error.WriteLine("Abort: the database already contains users. Provisioning is only permitted on an empty database.");
                return 1;
            }

            // Department: create one only if none exists. Ensure the selected department is active and valid.
            var departments = await departmentRepository.ListAllAsync();
            Department? department;
            if (departments.Count == 0)
            {
                department = Department.Create(DefaultDepartmentName, DefaultDepartmentCode, isActive: true);
                await departmentRepository.AddAsync(department);
                Console.WriteLine($"Created department: {department.Name} ({department.Code})");
            }
            else
            {
                department = departments.FirstOrDefault(d => d.IsActive);
                if (department is null)
                {
                    Console.Error.WriteLine("Abort: no active department found in the database.");
                    return 1;
                }
                Console.WriteLine($"Using existing department: {department.Name} ({department.Code})");
            }

            // Collect Admin details from the user. Password is read without echo.
            Console.WriteLine();
            Console.WriteLine("Provision the initial Admin user:");
            Console.WriteLine("--------------------------------");

            string emailInput = PromptRequired("Work email: ");
            var email = Email.Create(emailInput);

            string fullNameInput = PromptRequired("Full name (First Last): ");
            var name = FullName.Parse(fullNameInput);

            string employeeCodeInput = PromptRequired("Employee code (e.g. EMP000001): ");
            var employeeCode = EmployeeCode.Create(employeeCodeInput);

            string password = ReadPassword("Admin password: ");
            Console.WriteLine();

            // Build the user through the existing domain model. Role = Admin, active, hashed credential.
            var user = User.Rehydrate(
                id: default,
                publicId: Guid.NewGuid(),
                name: name,
                email: email,
                employeeCode: employeeCode,
                departmentId: department.PublicId,
                isActive: true,
                role: UserRole.Admin,
                passwordHash: passwordHasher.Hash(password));

            await userRepository.AddAsync(user);
            await unitOfWork.SaveChangesAsync();

            // Non-sensitive confirmation only. Never print the password or hash.
            Console.WriteLine();
            Console.WriteLine("Admin provisioned successfully.");
            Console.WriteLine($"  Email:    {user.Email.Value}");
            Console.WriteLine($"  Role:     {user.Role}");
            Console.WriteLine($"  Active:   {user.IsActive}");
            Console.WriteLine($"  PublicId: {user.PublicId}");
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Provisioning failed: {ex.Message}");
            return 1;
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    private static string PromptRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            Console.Error.WriteLine("  This field is required. Please try again.");
        }
    }

    /// <summary>
    /// Reads a password from the console without echoing characters to the screen.
    /// The plaintext password is held in memory only for hashing and is never logged.
    /// </summary>
    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var chars = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(chars.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0)
                {
                    chars.RemoveAt(chars.Count - 1);
                    Console.Write("\b \b");
                }
                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                throw new OperationCanceledException("Provisioning cancelled by the user.");
            }

            if (!char.IsControl(key.KeyChar))
            {
                chars.Add(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
