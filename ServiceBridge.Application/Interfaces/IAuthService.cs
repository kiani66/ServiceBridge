using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult> RegisterAsync(UserRegisterRequest request);
    Task<ServiceResult<LoginResponseDto>> LoginAsync(UserLoginRequest request);
}
