using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Project_LawyerSystem_CharpApi.Application.DTOs.Address;
using Project_LawyerSystem_CharpApi.Application.DTOs.Client;
using Project_LawyerSystem_CharpApi.Application.DTOs.Lawyer;
using Project_LawyerSystem_CharpApi.Application.DTOs.User;
using Project_LawyerSystem_CharpApi.Domain.Interfaces;
using Project_LawyerSystem_CharpApi.Domain.Models;
using Project_LawyerSystem_CharpApi.Infrastructure.Configurations;

namespace Project_LawyerSystem_CharpApi.Application.Services;

/// <summary>
/// Provides authentication-related services, including user registration, login, and token generation.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for accessing user data.</param>
    /// <param name="mapper">The mapper for object-to-object mapping.</param>
    /// <param name="configuration"></param>
    public AuthService(
        IUserRepository userRepository,
        IMapper mapper,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers a full lawyer user, including their address and lawyer details.
    /// </summary>
    /// <param name="userDto">The user details to register.</param>
    /// <param name="addressDto">The address details of the user.</param>
    /// <param name="lawyerDto">The lawyer-specific details of the user.</param>
    /// <returns>A <see cref="UserReadDto"/> representing the registered user.</returns>
    /// <exception cref="Exception">Thrown when there are issues during registration.</exception>
    public async Task<UserReadDto> RegisterFulLawyerlUser(
        UserCreateDto userDto,
        AddressDto addressDto,
        LawyerCreateDto lawyerDto)
    {
        using var transaction = await _userRepository.BeginTransactionAsync();

        try
        {
            var address = _mapper.Map<Address>(addressDto);

            address.CreatedAt = DateTime.UtcNow;
            address.UpdatedAt = DateTime.UtcNow;

            var dbResult = await _userRepository.AddAddressAsync(address);

            if (dbResult == 0)
            {
                throw new Exception("Erro ao cadastrar usuario");
            }

            var lawyer = _mapper.Map<Lawyer>(lawyerDto);

            lawyer.CreatedAt = DateTime.UtcNow;
            lawyer.UpdatedAt = DateTime.UtcNow;

            if (await _userRepository.GetLawyerByOabAsync(lawyer.OAB) != null)
            {
                throw new Exception("Essa OAB ja esta em uso!");
            }

            await _userRepository.AddLawyerAsync(lawyer);

            var user = _mapper.Map<User>(userDto);

            await VerifyUser(user);

            user.AddressId = address.Id;
            user.LawyerOAB = lawyer.OAB;

            var salt = CryptoHelper.GenerateSalt();

            var hash = CryptoHelper.HashPassword(user.Password, salt);

            user.Salt = salt;
            user.Password = hash;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            if (await _userRepository.AddUserAsync(user) == 0)
            {
                throw new Exception("Erro ao cadastrar usuario");
            }

            await transaction.CommitAsync();

            return _mapper.Map<UserReadDto>(user);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Register full client.
    /// </summary>
    /// <param name="userCreateDto">The user details to register.</param>
    /// <param name="addressDto">The address details of the user.</param>
    /// <param name="clientDto">The client-specific details of the user.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<UserReadDto> RegisterFullClientUser(
                                        UserCreateDto userCreateDto,
                                        AddressDto addressDto,
                                        ClientDto clientDto)
    {
        using var transaction = await _userRepository.BeginTransactionAsync();
        try
        {
            var address = _mapper.Map<Address>(addressDto);
            address.CreatedAt = DateTime.UtcNow;
            address.UpdatedAt = DateTime.UtcNow;
            var dbResult = await _userRepository.AddAddressAsync(address);

            if (dbResult == 0)
            {
                throw new Exception("Erro ao cadastrar usuario");
            }

            var client = _mapper.Map<Client>(clientDto);
            client.CreatedAt = DateTime.UtcNow;
            client.UpdatedAt = DateTime.UtcNow;
            dbResult = await _userRepository.AddClientAsync(client);

            if (dbResult == 0)
            {
                throw new Exception("Erro ao cadastrar usuario");
            }

            var user = _mapper.Map<User>(userCreateDto);

            await VerifyUser(user);

            user.AddressId = address.Id;
            user.ClientId = client.Id;

            var salt = CryptoHelper.GenerateSalt();

            var hash = CryptoHelper.HashPassword(user.Password, salt);

            user.Salt = salt;
            user.Password = hash;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            dbResult = await _userRepository.AddUserAsync(user);

            if (dbResult == 0)
            {
                throw new Exception("Erro ao cadastrar usuario");
            }

            await transaction.CommitAsync();

            return _mapper.Map<UserReadDto>(user);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Authenticates a user by validating their credentials and generates a JWT token.
    /// </summary>
    /// <param name="userLoginDto">The login details of the user, including email and password.</param>
    /// <returns>A JWT token if authentication is successful.</returns>
    /// <exception cref="ArgumentException">Thrown when the email is null or empty.</exception>
    /// <exception cref="Exception">Thrown when the email or password is incorrect.</exception>
    public async Task<(string token, User user)> LoginUser(UserLoginDto userLoginDto)
    {
        if (string.IsNullOrEmpty(userLoginDto.Email))
        {
            throw new ArgumentException(
                "Email cannot be null or empty",
                nameof(userLoginDto.Email));
        }

        Console.WriteLine("login: " + userLoginDto.Email);
        Console.WriteLine("Senha: " + userLoginDto.Password);

        var user = await _userRepository.GetUserByEmailAsync(userLoginDto.Email);

        if (user == null)
        {
            throw new Exception("Email nao existe");
        }

        if (user.Email != userLoginDto.Email)
        {
            throw new Exception("Email ou senha incorreto! Tente novamente!");
        }

        var result = CryptoHelper.VerifyPassword(
                                userLoginDto.Password,
                                user.Password,
                                user.Salt);

        if (!result)
        {
            throw new Exception("Email ou senha incorreto! Tente novamente!");
        }

        var token = GenerateToken(user);

        if (token == null)
        {
            throw new Exception("Falha ao gerar token");
        }

        if (user == null)
        {
            throw new Exception("Erro: Usuario e nulo!");
        }

        return (token, user);
    }

    public async Task UpdateUserLawyerAsync(
        UserUpdateDto? userUpdateDto,
        LawyerUpdateDto? lawyerUpdateDto,
        AddressDto? addressDto)
    {
        var user = await _userRepository.GetUserByEmailAsync(userUpdateDto.Email);

        if (userUpdateDto.Name != null)
        {
            user.Name = userUpdateDto.Name;
        }

        if (userUpdateDto.Phone != null)
        {
            user.Phone = userUpdateDto.Phone;
        }

        if (userUpdateDto.Password != null)
        {
            var salt = CryptoHelper.GenerateSalt();
            var hash = CryptoHelper.HashPassword(userUpdateDto.Password, salt);

            user.Salt = salt;
            user.Password = hash;
        }
        var lawyer = await _userRepository.GetLawyerByOabAsync(user.LawyerOAB);

        if (lawyerUpdateDto.AreaOfExpertise != null)
        {
            lawyer.AreaOfExpertise = lawyerUpdateDto.AreaOfExpertise;
        }

        var address = await _userRepository.GetAddressByIdAsync(user.AddressId);

        if (addressDto.Street != null)
        {
            address.Street = addressDto.Street;
        }

        if (addressDto.City != null)
        {
            address.City = addressDto.City;
        }

        if (addressDto.State != null)
        {
            address.State = addressDto.State;
        }

        if (addressDto.ZipCode != null)
        {
            address.ZipCode = addressDto.ZipCode;
        }

        if (addressDto.Complement != null)
        {
            address.Complement = addressDto.Complement;
        }

        if (addressDto.Number != null)
        {
            address.Number = addressDto.Number;
        }

        if (addressDto.Neighborhood != null)
        {
            address.Neighborhood = addressDto.Neighborhood;
        }

        user.UpdatedAt = DateTime.UtcNow;
        lawyer.UpdatedAt = DateTime.UtcNow;
        address.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Verifiy if the user is arleady registered in the system.
    /// </summary>
    /// <param name="user">the user to verify.</param>
    /// <exception cref="NullReferenceException">Case user is null.</exception>
    /// <exception cref="Exception">Case user is already registered in the system.</exception>
    private async Task VerifyUser(User user)
    {
        if (user == null)
        {
            throw new NullReferenceException(nameof(user));
        }

        if (await _userRepository.GetUserByEmailAsync(user.Email) != null)
        {
            throw new Exception("Esse email ja esta em uso!");
        }
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="user">The user for whom the token is generated.</param>
    /// <returns>A JWT token as a string.</returns>
    private string GenerateToken(User user)
    {
        //DotNetEnv.Env.Load();

        var secretKey = _configuration["secretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null or empty.");
        }

        var claims = new[]
        {
           new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
           new Claim(ClaimTypes.Email, user.Email.ToString()),
           new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "http://localhost:5000",
            audience: "http://localhost:5000",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task UpdateUserClientAsync(
        UserUpdateDto? userUpdate,
        AddressDto? addressDto,
        ClientDto? clientDto)
    {
        using var transaction = await _userRepository.BeginTransactionAsync();

        try
        {
            var user = await _userRepository.GetUserByEmailAsync(userUpdate.Email);
            if (user == null)
            {
                throw new Exception("Usuario nao encontrado!");
            }
            if (userUpdate.Name != null)
            {
                user.Name = userUpdate.Name;
            }
            if (userUpdate.Phone != null)
            {
                user.Phone = userUpdate.Phone;
            }
            if (userUpdate.Password != null)
            {
                var salt = CryptoHelper.GenerateSalt();
                var hash = CryptoHelper.HashPassword(userUpdate.Password, salt);
                user.Salt = salt;
                user.Password = hash;
            }
            user.UpdatedAt = DateTime.UtcNow;

            var address = await _userRepository.GetAddressByIdAsync(user.AddressId);
            if (address == null)
            {
                throw new Exception("Endereco nao encontrado!");
            }

            if (addressDto.Street != null)
            {
                address.Street = addressDto.Street;
            }

            if (addressDto.City != null)
            {
                address.City = addressDto.City;
            }

            if (addressDto.State != null)
            {
                address.State = addressDto.State;
            }

            if (addressDto.ZipCode != null)
            {
                address.ZipCode = addressDto.ZipCode;
            }

            if (addressDto.Complement != null)
            {
                address.Complement = addressDto.Complement;
            }

            if (addressDto.Number != null)
            {
                address.Number = addressDto.Number;
            }

            if (addressDto.Neighborhood != null)
            {
                address.Neighborhood = addressDto.Neighborhood;
            }

            var client = await _userRepository.GetClientByIdAsync(user.ClientId);

            if (client == null)
            {
                throw new Exception("Cliente nao encontrado!");
            }

            if (clientDto.Profission != null)
            {
                client.Profission = clientDto.Profission;
            }
            if (clientDto.MaritalStatus != null)
            {
                client.MaritalStatus = clientDto.MaritalStatus;
            }

            if (clientDto.Representative != null)
            {
                client.Representative = clientDto.Representative;
            }

            if (client.CompanyName != null)
            {
                client.CompanyName = clientDto.CompanyName;
            }

            await _userRepository.SaveChangesAsync();

            await transaction.CommitAsync();

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception(ex.Message);
        }

    }
}
