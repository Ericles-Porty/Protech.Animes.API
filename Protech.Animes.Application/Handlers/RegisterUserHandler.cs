using MediatR;
using Protech.Animes.Application.Commands;
using Protech.Animes.Application.DTOs;
using Protech.Animes.Application.Interfaces;
using Protech.Animes.Domain.Entities;
using Protech.Animes.Domain.Exceptions;

namespace Protech.Animes.Application.Handlers;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, UserDto>
{

    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICryptographyService _cryptographyService;

    public RegisterUserHandler(IUserService userService, IJwtTokenService jwtTokenService, ICryptographyService cryptographyService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _cryptographyService = cryptographyService;
    }

    public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (request.RegisterUserDto.Password != request.RegisterUserDto.ConfirmPassword) throw new BadRequestException("Passwords do not match");

        var userExists = await _userService.GetByEmail(request.RegisterUserDto.Email);
        if (userExists is not null) throw new DuplicatedEntityException("User already exists");

        var hashedPassword = _cryptographyService.Encrypt(request.RegisterUserDto.Password);
        var hashedPasswordBytes = System.Text.Encoding.UTF8.GetBytes(hashedPassword);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.RegisterUserDto.Name,
            Email = request.RegisterUserDto.Email,
            Password = hashedPasswordBytes
        };

        var createdUser = await _userService.Register(user);
        var jwtToken = _jwtTokenService.GenerateToken(createdUser);
        var userDto = new UserDto
        {
            Name = createdUser.Name,
            Email = createdUser.Email,
            Token = jwtToken
        };

        return userDto;
    }
}