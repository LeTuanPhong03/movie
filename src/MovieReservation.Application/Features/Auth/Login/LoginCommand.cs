using MediatR;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;