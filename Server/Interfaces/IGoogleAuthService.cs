using Google.Apis.Auth;
using Server.Models;

namespace Server.Interfaces;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken);
    Task<User> HandleGoogleUser(GoogleJsonWebSignature.Payload payload);
}
