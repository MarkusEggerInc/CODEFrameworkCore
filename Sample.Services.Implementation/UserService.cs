using Sample.Contracts;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

namespace Sample.Services.Implementation
{
    public class UserService : IUserService
    {

        /// <summary>
        /// Simulate User Principal 
        /// </summary>
        public ClaimsPrincipal User
        {
            get
            {
                if (_user == null)
                    _user = Thread.CurrentPrincipal as ClaimsPrincipal;

                return _user;
            }
        }
        private ClaimsPrincipal _user = null;

     

        public UserService()
        {
            
        }


        public AuthenticateUserResponse AuthenticateUser(AuthenticateUserRequest request)
        {
            try
            {
                var response = new AuthenticateUserResponse();

                if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                {
                    response.SetError("Invalid username or password.");
                    return response;
                }

              

                response.Id = Guid.NewGuid();
                response.Email = "test@user.com";
                response.Firstname = "Test";
                response.Lastname = "User";
                
                
                // passthrough success and set Auth cooki in controller override
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                return new AuthenticateUserResponse { Success = false, FailureInformation = "Generic error in UserService::AuthenticateUser" };
            }
        }

        public SignoutResponse Signout(SignoutRequest response)
        {
            // passthrough success and check in controller override
            return new SignoutResponse();
        }

        public IsAuthenticatedResponse IsAuthenticated(IsAuthenticatedRequest request)
        {
            // passthrough success and check in controller override
            return new IsAuthenticatedResponse
            {
                Success = true,
                Username = "Bogus",
                UserId = "12345"
            };
        }


        public GetUserResponse GetUser(GetUserRequest request)
        {
            var response = new GetUserResponse();
            response.UserId = request.Id;
            response.Success = true;
            response.Firstname = "Test";
            response.Lastname = "User";
            response.Email = "test@user.com";
            
            return response;
        }



        /// <summary>
        /// Saves a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public SaveUserResponse SaveUser(SaveUserRequest userInfo)
        {
            
              return new SaveUserResponse { Success = true, Id = Guid.NewGuid() };
            
        }

        public ResetPasswordResponse ResetPassword(ResetPasswordRequest request)
        {
            var response = new ResetPasswordResponse();
            response.Success = true;
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

        public GetUsersResponse GetUsers(GetUsersRequest request)
        {
            var response = new GetUsersResponse();

            var userList = new List<User>()
            {
                new Contracts.User
                {
                    Id = Guid.NewGuid(),
                    Username = "rstrahl"
                },
                new Contracts.User
                {
                    Id = Guid.NewGuid(),
                    Username = "megger"
                }
            };

            response.Users = userList;
            return response;
        }
    }

}
