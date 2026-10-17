using MediatR;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Auth.Register;
public record RegisterCommand(string FullName, string Email, string Password, string ConfirmPassword) : IRequest<AuthResponse>;