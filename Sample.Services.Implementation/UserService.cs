using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CODE.Framework.Fundamentals.Utilities;
using CODE.Framework.Services.Contracts;
using Sample.Contracts;

namespace Sample.Services.Implementation
{
    public class UserService : IUserService
    {
        public AuthenticateUserResponse AuthenticateUser(AuthenticateUserRequest request)
        {
            try
            {
                var response = new AuthenticateUserResponse();

                if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                {
                    response.Success = false;
                    response.FailureInformation = "Invalid username or password.";
                    return response;
                }

                response.Id = Guid.NewGuid();
                response.Email = "test@user.com";
                response.Firstname = "Test";
                response.Lastname = "User";

                // pass-through success and set Auth cookie in controller override
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                return ServiceHelper.GetPopulatedFailureResponse<AuthenticateUserResponse>(ex);
            }
        }

        public SignoutResponse Signout(SignoutRequest response) => new SignoutResponse { Success = true };

        public IsAuthenticatedResponse IsAuthenticated(IsAuthenticatedRequest request)
        {
            var user = this.GetCurrentPrincipal();

            var success = user.Identity.IsAuthenticated;
            string username = null;
            if (success && user.Identity is ClaimsIdentity id)
                username = id.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            // pass-through success and check in controller override
            return new IsAuthenticatedResponse
            {
                Success = success,
                Username = username
            };
        }

        public GetUserResponse GetUser(GetUserRequest request) =>
            new GetUserResponse
            {
                UserId = request.Id,
                Success = true,
                Firstname = "Test",
                Lastname = "User",
                Email = "test@user.com"
            };

        public SaveUserResponse SaveUser(SaveUserRequest userInfo) => new SaveUserResponse {Success = true, Id = Guid.NewGuid()};

        public ResetPasswordResponse ResetPassword(ResetPasswordRequest request)
        {
            var response = new ResetPasswordResponse {Success = true};
            return response;

            //            var user = UserRepository.GetUserByUsername(request.Username);
            //            if (user == null)
            //            {
            //                response.Success = false;
            //                response.FailureInformation = "Invalid username.";
            //            }
            //            else
            //            {
            //                string password = DataUtils.GenerateUniqueId(10);
            //                user.Password = password;
            //                UserRepository.SaveUser(user);

            //                // TODO: Use Code Framework Configuration Settings and Email Sending?
            //                //       or Westwind.Utilities.Configuration?
            //                string appName = ConfigurationManager.AppSettings["AppName"] ?? "Wikinome";
            //                var smtp = new SmtpClientNative();
            //                smtp.MailServer = ConfigurationManager.AppSettings["MailServer"];
            //                smtp.Username = ConfigurationManager.AppSettings["MailUsername"];
            //                smtp.Password = ConfigurationManager.AppSettings["MailPassword"];
            //                string useSsl = ConfigurationManager.AppSettings["MailUseSsl"];
            //                smtp.SenderEmail = ConfigurationManager.AppSettings["MailSenderEmail"];
            //                if (!string.IsNullOrEmpty(useSsl) && useSsl.ToLower() == "true")
            //                    smtp.UseSsl = true;

            //                smtp.Subject = appName + " Password Recovery";
            //                smtp.Recipient = user.Username;
            //                smtp.Message = $@"This message contains a temporary password so you can reset your password 
            //Please use this temporary password to sign in, then access your account profile and change 
            //the password to something you can remember.

            //Your temporary password is: {password}

            //The {appName} Team";

            //                if (!smtp.SendMail())
            //                {
            //                    response.Success = false;
            //                    response.FailureInformation = "Failed to send password reset email: " + smtp.ErrorMessage;
            //                }
            //            }

            //            return response;
        }

        public GetUsersResponse GetUsers(GetUsersRequest request) => new GetUsersResponse
        {
            Users = new List<User>
            {
                new User {Id = Guid.NewGuid(), Username = "megger"},
                new User {Id = Guid.NewGuid(), Username = "myeager"},
                new User {Id = Guid.NewGuid(), Username = "ewhitney"},
                new User {Id = Guid.NewGuid(), Username = "odobretsberger"}
            },
            Success = true
        };

        public DateTime Test(string bla)
        {
            return DateTime.Now;
        }
    }
}